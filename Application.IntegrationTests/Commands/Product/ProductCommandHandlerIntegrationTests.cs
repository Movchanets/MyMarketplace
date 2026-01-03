using Application.Commands.Product.CreateProduct;
using Application.Commands.Product.DeleteProduct;

using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.IntegrationTests.Commands.Product;

public class ProductCommandHandlerIntegrationTests : TestBase
{
	private static async Task<Domain.Entities.User> CreateDomainUserAsync(AppDbContext db)
	{
		var user = new Domain.Entities.User(Guid.NewGuid(), email: $"user_{Guid.NewGuid():N}@example.com");
		db.DomainUsers.Add(user);
		await db.SaveChangesAsync();
		return user;
	}

	private async Task<Domain.Entities.Store> CreateStoreAsync(Guid userId, bool suspended = false)
	{
		var store = Domain.Entities.Store.Create(userId, "My Store", null);
		if (suspended)
		{
			store.Suspend();
		}

		DbContext.Stores.Add(store);
		await DbContext.SaveChangesAsync();
		return store;
	}

	private async Task<Domain.Entities.Category> CreateCategoryAsync(string name)
	{
		var category = Domain.Entities.Category.Create(name);
		DbContext.Categories.Add(category);
		await DbContext.SaveChangesAsync();
		return category;
	}

	private async Task<Domain.Entities.Tag> CreateTagAsync(string name)
	{
		var tag = Domain.Entities.Tag.Create(name);
		DbContext.Tags.Add(tag);
		await DbContext.SaveChangesAsync();
		return tag;
	}

	[Fact]
	public async Task CreateProduct_ShouldPersistProductWithSkuCategoryAndTags()
	{
		// Arrange
		var user = await CreateDomainUserAsync(DbContext);
		await CreateStoreAsync(user.Id);
		var category1 = await CreateCategoryAsync("Electronics");
		var category2 = await CreateCategoryAsync("Accessories");
		var tag = await CreateTagAsync("Hot");

		var storeRepo = new StoreRepository(DbContext);
		var productRepo = new ProductRepository(DbContext);
		var userRepo = new UserRepository(DbContext);
		var categoryRepo = new CategoryRepository(DbContext);
		var tagRepo = new TagRepository(DbContext);
		var uow = new UnitOfWork(DbContext);
		var handler = new CreateProductCommandHandler(
			productRepo,
			storeRepo,
			userRepo,
			categoryRepo,
			tagRepo,
			uow,
			NullLogger<CreateProductCommandHandler>.Instance);

		var cmd = new CreateProductCommand(
			user.IdentityUserId,
			"iPhone",
			"desc",
			CategoryIds: new List<Guid> { category1.Id, category2.Id },
			Price: 999.99m,
			StockQuantity: 5,
			Attributes: new Dictionary<string, object?> { ["color"] = "black" },
			TagIds: new List<Guid> { tag.Id });

		// Act
		var res = await handler.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeTrue();
		res.Payload.Should().NotBe(Guid.Empty);

		var product = await DbContext.Products
			.Include(p => p.Skus)
			.Include(p => p.ProductCategories)
			.Include(p => p.ProductTags)
			.SingleAsync(p => p.Id == res.Payload);

		product.StoreId.Should().NotBe(Guid.Empty);
		product.Name.Should().Be("iPhone");
		product.Description.Should().Be("desc");

		product.Skus.Should().HaveCount(1);
		product.Skus.Single().Price.Should().Be(999.99m);
		product.Skus.Single().StockQuantity.Should().Be(5);

		product.ProductCategories.Should().Contain(pc => pc.CategoryId == category1.Id);
		product.ProductCategories.Should().Contain(pc => pc.CategoryId == category2.Id);
		product.ProductTags.Should().ContainSingle(pt => pt.TagId == tag.Id);
	}

	[Fact]
	public async Task CreateProduct_WhenStoreSuspended_ShouldFail_AndNotPersist()
	{
		// Arrange
		var user = await CreateDomainUserAsync(DbContext);
		await CreateStoreAsync(user.Id, suspended: true);
		var category = await CreateCategoryAsync("Electronics");

		var storeRepo = new StoreRepository(DbContext);
		var productRepo = new ProductRepository(DbContext);
		var userRepo = new UserRepository(DbContext);
		var categoryRepo = new CategoryRepository(DbContext);
		var tagRepo = new TagRepository(DbContext);
		var uow = new UnitOfWork(DbContext);
		var handler = new CreateProductCommandHandler(
			productRepo,
			storeRepo,
			userRepo,
			categoryRepo,
			tagRepo,
			uow,
			NullLogger<CreateProductCommandHandler>.Instance);

		var cmd = new CreateProductCommand(user.IdentityUserId, "iPhone", null, category.Id, 1, 1);

		// Act
		var res = await handler.Handle(cmd, CancellationToken.None);

		// Assert
		res.IsSuccess.Should().BeFalse();
		res.Message.Should().Be("Store is suspended");
		(await DbContext.Products.CountAsync()).Should().Be(0);
		(await DbContext.Skus.CountAsync()).Should().Be(0);
	}


	[Fact]
	public async Task DeleteProduct_ShouldRemoveProductAndSkus()
	{
		// Arrange
		var user = await CreateDomainUserAsync(DbContext);
		await CreateStoreAsync(user.Id);
		var category = await CreateCategoryAsync("Electronics");

		var storeRepo = new StoreRepository(DbContext);
		var productRepo = new ProductRepository(DbContext);
		var userRepo = new UserRepository(DbContext);
		var categoryRepo = new CategoryRepository(DbContext);
		var tagRepo = new TagRepository(DbContext);
		var uow = new UnitOfWork(DbContext);

		var createHandler = new CreateProductCommandHandler(
			productRepo,
			storeRepo,
			userRepo,
			categoryRepo,
			tagRepo,
			uow,
			NullLogger<CreateProductCommandHandler>.Instance);

		var createRes = await createHandler.Handle(
			new CreateProductCommand(user.IdentityUserId, "To Delete", null, category.Id, 10, 2),
			CancellationToken.None);

		createRes.IsSuccess.Should().BeTrue();
		(await DbContext.Products.CountAsync()).Should().Be(1);
		(await DbContext.Skus.CountAsync()).Should().Be(1);

		var deleteHandler = new DeleteProductCommandHandler(
			productRepo,
			storeRepo,
			userRepo,
			uow,
			NullLogger<DeleteProductCommandHandler>.Instance);

		// Mimic a new request scope
		DbContext.ChangeTracker.Clear();

		// Act
		var deleteRes = await deleteHandler.Handle(new DeleteProductCommand(user.IdentityUserId, createRes.Payload), CancellationToken.None);

		// Assert
		deleteRes.IsSuccess.Should().BeTrue();
		deleteRes.Message.Should().Be("Product deleted successfully");
		(await DbContext.Products.CountAsync()).Should().Be(0);
		(await DbContext.Skus.CountAsync()).Should().Be(0);
	}
}
