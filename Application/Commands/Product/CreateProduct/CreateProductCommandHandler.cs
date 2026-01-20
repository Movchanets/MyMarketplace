using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Product.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ServiceResponse<Guid>>
{
	private readonly IProductRepository _productRepository;
	private readonly IStoreRepository _storeRepository;
	private readonly IUserRepository _userRepository;
	private readonly ICategoryRepository _categoryRepository;
	private readonly ITagRepository _tagRepository;
	private readonly IAttributeDefinitionRepository _attributeDefinitionRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<CreateProductCommandHandler> _logger;

	public CreateProductCommandHandler(
		IProductRepository productRepository,
		IStoreRepository storeRepository,
		IUserRepository userRepository,
		ICategoryRepository categoryRepository,
		ITagRepository tagRepository,
		IAttributeDefinitionRepository attributeDefinitionRepository,
		IUnitOfWork unitOfWork,
		ILogger<CreateProductCommandHandler> logger)
	{
		_productRepository = productRepository;
		_storeRepository = storeRepository;
		_userRepository = userRepository;
		_categoryRepository = categoryRepository;
		_tagRepository = tagRepository;
		_attributeDefinitionRepository = attributeDefinitionRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating product {ProductName} for user {UserId}", request.Name, request.UserId);

		try
		{
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser is null)
			{
				_logger.LogWarning("Domain user for identity {UserId} not found", request.UserId);
				return new ServiceResponse<Guid>(false, "User not found");
			}

			var store = await _storeRepository.GetByUserIdAsync(domainUser.Id);
			if (store == null)
			{
				_logger.LogWarning("Store for user {UserId} not found", domainUser.Id);
				return new ServiceResponse<Guid>(false, "Store not found");
			}

			if (store.IsSuspended)
			{
				_logger.LogWarning("Store {StoreId} is suspended", store.Id);
				return new ServiceResponse<Guid>(false, "Store is suspended");
			}

			var product = new Domain.Entities.Product(request.Name, request.Description);
			store.AddProduct(product);

			var categoryIds = (request.CategoryIds ?? new List<Guid>())
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToList();

			foreach (var categoryId in categoryIds)
			{
				var category = await _categoryRepository.GetByIdAsync(categoryId);
				if (category == null)
				{
					_logger.LogWarning("Category {CategoryId} not found", categoryId);
					return new ServiceResponse<Guid>(false, "Category not found");
				}

				product.AddCategory(category);
			}

			if (request.TagIds is { Count: > 0 })
			{
				foreach (var tagId in request.TagIds.Distinct())
				{
					var tag = await _tagRepository.GetByIdAsync(tagId);
					if (tag == null)
					{
						_logger.LogWarning("Tag {TagId} not found", tagId);
						return new ServiceResponse<Guid>(false, "Tag not found");
					}

					product.AddTag(tag);
				}
			}

		var sku = SkuEntity.Create(product.Id, request.Price, request.StockQuantity, request.Attributes);
		product.AddSku(sku);
		
		// Convert JSONB attributes to typed attributes
		if (request.Attributes != null && request.Attributes.Count > 0)
		{
			var attributeDefinitions = await _attributeDefinitionRepository.GetAllAsync();
			sku.SetTypedAttributes(request.Attributes, attributeDefinitions);
		}

		_productRepository.Add(product);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Product {ProductId} created successfully", product.Id);
			return new ServiceResponse<Guid>(true, "Product created successfully", product.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating product for user {UserId}", request.UserId);
			return new ServiceResponse<Guid>(false, $"Error: {ex.Message}");
		}
	}
}
