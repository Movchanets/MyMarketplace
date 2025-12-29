using System;
using Infrastructure.Entities.Identity;
using Domain.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Domain.Entities;
using Domain.Helpers;
using Domain.Interfaces.Repositories;
using Application.Interfaces;

namespace Infrastructure.Initializer;

public static class SeederDB
{
    private const string AdminEmail = "admin@example.com";

    private static async Task AddClaimToRoleIfNotExists(RoleManager<RoleEntity> roleManager, RoleEntity role, string type, string value)
    {
        var existingClaims = await roleManager.GetClaimsAsync(role);
        if (!existingClaims.Any(c => c.Type == type && c.Value == value))
        {
            await roleManager.AddClaimAsync(role, new Claim(type, value));
        }
    }

    private static async Task EnsureRoleExists(RoleManager<RoleEntity> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new RoleEntity { Name = roleName });
        }
    }

    private static async Task EnsureRolesAndClaims(RoleManager<RoleEntity> roleManager)
    {
        await EnsureRoleExists(roleManager, Roles.Admin);
        await EnsureRoleExists(roleManager, Roles.User);
        await EnsureRoleExists(roleManager, Roles.Seller);

        var adminRole = await roleManager.FindByNameAsync(Roles.Admin);
        if (adminRole != null)
        {
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "users.manage");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "users.read");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "users.update");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "users.delete");
            // Roles management
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "roles.manage");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "roles.read");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "roles.create");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "roles.update");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "roles.delete");
            // Stores
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "stores.manage");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "stores.verify");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "stores.read.all");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "stores.suspend");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "stores.delete");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "categories.manage");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "tags.manage");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "manage_catalog");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "payouts.read.all");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "payouts.process");
        }

        var sellerRole = await roleManager.FindByNameAsync(Roles.Seller);
        if (sellerRole != null)
        {
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "products.create");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "products.read.self");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "products.update.self");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "products.delete.self");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "orders.read.self");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "orders.update.status");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "store.update.self");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "store.read.self");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "payouts.read.self");
            await AddClaimToRoleIfNotExists(roleManager, sellerRole, "permission", "payouts.request");
        }

        var userRole = await roleManager.FindByNameAsync(Roles.User);
        if (userRole != null)
        {
            await AddClaimToRoleIfNotExists(roleManager, userRole, "permission", "orders.create");
            await AddClaimToRoleIfNotExists(roleManager, userRole, "permission", "orders.read.self");
            await AddClaimToRoleIfNotExists(roleManager, userRole, "permission", "reviews.create");
            await AddClaimToRoleIfNotExists(roleManager, userRole, "permission", "reviews.update.self");
            await AddClaimToRoleIfNotExists(roleManager, userRole, "permission", "profile.read.self");
            await AddClaimToRoleIfNotExists(roleManager, userRole, "permission", "profile.update.self");
        }
    }

    private static async Task<Category> EnsureCategoryAsync(AppDbContext dbContext, string name, string? description = null, Guid? parentCategoryId = null)
    {
        var slug = SlugHelper.GenerateSlug(name);
        var existing = await dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (existing != null)
        {
            return existing;
        }

        var category = Category.Create(name, description, parentCategoryId);
        dbContext.Categories.Add(category);
        return category;
    }

    private static async Task<Tag> EnsureTagAsync(AppDbContext dbContext, string name, string? description = null)
    {
        var slug = SlugHelper.GenerateSlug(name);
        var existing = await dbContext.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
        if (existing != null)
        {
            return existing;
        }

        var tag = Tag.Create(name, description);
        dbContext.Tags.Add(tag);
        return tag;
    }

    private static async Task<MediaImage> EnsureMediaImageAsync(AppDbContext dbContext, string storageKey, string mimeType, int width, int height, string? altText = null)
    {
        var existing = await dbContext.MediaImages.FirstOrDefaultAsync(m => m.StorageKey == storageKey);
        if (existing != null)
        {
            return existing;
        }

        var media = new MediaImage(storageKey, mimeType, width, height, altText);
        dbContext.MediaImages.Add(media);
        return media;
    }

    private static async Task<AttributeDefinition> EnsureAttributeDefinitionAsync(
        AppDbContext dbContext,
        string code,
        string name,
        string dataType = "string",
        bool isRequired = false,
        bool isVariant = false,
        string? description = null,
        string? unit = null,
        int displayOrder = 0,
        IEnumerable<string>? allowedValues = null)
    {
        var normalizedCode = code.ToLowerInvariant();
        var existing = await dbContext.AttributeDefinitions.FirstOrDefaultAsync(a => a.Code == normalizedCode);
        if (existing != null)
        {
            return existing;
        }

        var attribute = new AttributeDefinition(code, name, dataType, isRequired, isVariant, description, unit, displayOrder);
        if (allowedValues != null)
        {
            attribute.SetAllowedValues(allowedValues);
        }

        dbContext.AttributeDefinitions.Add(attribute);
        return attribute;
    }

    private static async Task SeedAttributeDefinitionsAsync(AppDbContext dbContext)
    {
        // Common product attributes
        await EnsureAttributeDefinitionAsync(dbContext,
            code: "color",
            name: "Color",
            dataType: "string",
            isVariant: true,
            description: "Product color variant",
            displayOrder: 1,
            allowedValues: new[] { "Black", "White", "Red", "Blue", "Green", "Gray", "Silver", "Gold" });

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "size",
            name: "Size",
            dataType: "string",
            isVariant: true,
            description: "Size variant (clothing, footwear)",
            displayOrder: 2,
            allowedValues: new[] { "XS", "S", "M", "L", "XL", "XXL", "XXXL" });

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "material",
            name: "Material",
            dataType: "string",
            isVariant: false,
            description: "Product material",
            displayOrder: 3);

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "brand",
            name: "Brand",
            dataType: "string",
            isRequired: true,
            isVariant: false,
            description: "Product brand/manufacturer",
            displayOrder: 4);

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "weight",
            name: "Weight",
            dataType: "number",
            isVariant: false,
            description: "Product weight",
            unit: "kg",
            displayOrder: 5);

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "storage",
            name: "Storage Capacity",
            dataType: "string",
            isVariant: true,
            description: "Storage capacity for electronics",
            displayOrder: 6,
            allowedValues: new[] { "16GB", "32GB", "64GB", "128GB", "256GB", "512GB", "1TB", "2TB" });

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "ram",
            name: "RAM",
            dataType: "string",
            isVariant: true,
            description: "RAM capacity for electronics",
            displayOrder: 7,
            allowedValues: new[] { "4GB", "8GB", "16GB", "32GB", "64GB" });

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "screen_size",
            name: "Screen Size",
            dataType: "number",
            isVariant: false,
            description: "Display diagonal size",
            unit: "inch",
            displayOrder: 8);

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "warranty",
            name: "Warranty",
            dataType: "string",
            isVariant: false,
            description: "Warranty period",
            displayOrder: 9,
            allowedValues: new[] { "6 months", "1 year", "2 years", "3 years", "5 years" });

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "connector",
            name: "Connector Type",
            dataType: "string",
            isVariant: true,
            description: "Cable/connector type",
            displayOrder: 10,
            allowedValues: new[] { "USB-A", "USB-C", "Lightning", "Micro-USB", "HDMI", "DisplayPort" });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCatalogDemoDataAsync(AppDbContext dbContext, IHostEnvironment env)
    {
        // Demo data is useful for local dev and automated tests.
        if (!(env.IsDevelopment() || env.IsEnvironment("Testing")))
        {
            return;
        }

        var adminUser = await dbContext.DomainUsers.FirstOrDefaultAsync(u => u.Email == AdminEmail);
        if (adminUser is null)
        {
            return;
        }

        var categories = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase)
        {
            ["electronics"] = await EnsureCategoryAsync(dbContext, "Electronics", "Gadgets and devices"),
            ["accessories"] = await EnsureCategoryAsync(dbContext, "Accessories", "Cables, cases, add-ons"),
            ["clothing"] = await EnsureCategoryAsync(dbContext, "Clothing", "Apparel and basics"),
        };

        var tags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase)
        {
            ["new"] = await EnsureTagAsync(dbContext, "New", "Recently added"),
            ["popular"] = await EnsureTagAsync(dbContext, "Popular", "Trending items"),
            ["sale"] = await EnsureTagAsync(dbContext, "Sale", "Discounted"),
        };

        await dbContext.SaveChangesAsync();

        var store = await dbContext.Stores.FirstOrDefaultAsync(s => s.UserId == adminUser.Id);
        if (store is null)
        {
            store = Store.Create(adminUser.Id, "Admin Store", "Seeded store for demo catalog");
            store.Verify();
            dbContext.Stores.Add(store);
            await dbContext.SaveChangesAsync();
        }

        async Task EnsureDemoProductAsync(
            string name,
            string? description,
            decimal price,
            int stock,
            IReadOnlyList<Category> productCategories,
            IReadOnlyList<Tag> productTags,
            Dictionary<string, object?>? skuAttributes,
            string baseImageUrl,
            (string StorageKey, string AltText)[] gallery)
        {
            var exists = await dbContext.Products.AnyAsync(p => p.StoreId == store.Id && p.Name == name);
            if (exists)
            {
                return;
            }

            var product = new Product(name, description);
            product.UpdateBaseImage(baseImageUrl);

            store.AddProduct(product);
            dbContext.Products.Add(product);

            foreach (var c in productCategories)
            {
                product.AddCategory(c);
            }

            foreach (var t in productTags)
            {
                product.AddTag(t);
            }

            var sku = SkuEntity.Create(product.Id, price, stock, skuAttributes);
            product.AddSku(sku);

            foreach (var (storageKey, altText) in gallery)
            {
                var media = await EnsureMediaImageAsync(dbContext, storageKey, "image/jpeg", 1200, 1200, altText);
                product.AddGalleryItem(media);
            }

            await dbContext.SaveChangesAsync();
        }

        await EnsureDemoProductAsync(
            name: "Phone Case (Demo)",
            description: "Seeded demo product with basic SKU attributes.",
            price: 12.99m,
            stock: 50,
            productCategories: new[] { categories["accessories"], categories["electronics"] },
            productTags: new[] { tags["popular"], tags["sale"] },
            skuAttributes: new Dictionary<string, object?> { ["color"] = "black", ["material"] = "silicone" },
            baseImageUrl: "https://picsum.photos/seed/appnet9-phone-case/800/800",
            gallery: new[]
            {
                ("seed/catalog/phone-case-1.jpg", "Phone case front"),
                ("seed/catalog/phone-case-2.jpg", "Phone case back"),
            });

        await EnsureDemoProductAsync(
            name: "Hoodie (Demo)",
            description: "Seeded demo apparel product.",
            price: 39.00m,
            stock: 20,
            productCategories: new[] { categories["clothing"] },
            productTags: new[] { tags["new"] },
            skuAttributes: new Dictionary<string, object?> { ["size"] = "M", ["color"] = "gray" },
            baseImageUrl: "https://picsum.photos/seed/appnet9-hoodie/800/800",
            gallery: new[]
            {
                ("seed/catalog/hoodie-1.jpg", "Hoodie front"),
                ("seed/catalog/hoodie-2.jpg", "Hoodie details"),
            });

        await EnsureDemoProductAsync(
            name: "USB-C Cable (Demo)",
            description: "Seeded demo accessory product.",
            price: 8.50m,
            stock: 100,
            productCategories: new[] { categories["accessories"] },
            productTags: new[] { tags["popular"] },
            skuAttributes: new Dictionary<string, object?> { ["length_m"] = 1.0, ["connector"] = "USB-C" },
            baseImageUrl: "https://picsum.photos/seed/appnet9-cable/800/800",
            gallery: new[]
            {
                ("seed/catalog/cable-1.jpg", "Cable"),
            });
    }

    public static async Task SeedDataAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<RoleEntity>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();


        await dbContext.Database.MigrateAsync();

        await EnsureRolesAndClaims(roleManager);

        // Seed attribute definitions (always, for all environments)
        await SeedAttributeDefinitionsAsync(dbContext);

        // створюємо адміністратора
        if (!userManager.Users.Any())
        {
            var adminEmail = AdminEmail;
            var admin = new ApplicationUser
            {
                Email = adminEmail,
                UserName = "admin",
                EmailConfirmed = true
            };

            using var transaction = await unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await userManager.CreateAsync(admin, "Qwerty-1!");
                if (result.Succeeded)
                {
                    // Тепер, коли EF згенерував Id для admin, створюємо доменного користувача з цим IdentityUserId
                    var domainAdmin = new User(admin.Id, "Admin", "User", adminEmail);
                    userRepository.Add(domainAdmin);
                    admin.DomainUserId = domainAdmin.Id;
                    await userManager.UpdateAsync(admin);
                    await userManager.AddToRoleAsync(admin, Roles.Admin);
                    await userManager.AddToRoleAsync(admin, Roles.User);
                    await unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error: {error.Code} - {error.Description}");
                    }
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // створюємо звичайного користувача
            var userEmail = "user@example.com";
            var user = new ApplicationUser
            {
                Email = userEmail,
                UserName = "user",
                EmailConfirmed = true
            };
            using var transaction2 = await unitOfWork.BeginTransactionAsync();
            try
            {
                var result2 = await userManager.CreateAsync(user, "User123!");
                if (result2.Succeeded)
                {
                    var domainUser = new User(user.Id, "Regular", "User", user.Email);
                    userRepository.Add(domainUser);
                    user.DomainUserId = domainUser.Id;
                    await userManager.UpdateAsync(user);
                    await userManager.AddToRoleAsync(user, Roles.User);
                    await unitOfWork.SaveChangesAsync();
                    await transaction2.CommitAsync();
                }
                else
                {
                    await transaction2.RollbackAsync();
                    foreach (var error in result2.Errors)
                    {
                        Console.WriteLine($"Error: {error.Code} - {error.Description}");
                    }
                }
            }
            catch
            {
                await transaction2.RollbackAsync();
                throw;
            }
        }

        await SeedCatalogDemoDataAsync(dbContext, env);

    }
}
