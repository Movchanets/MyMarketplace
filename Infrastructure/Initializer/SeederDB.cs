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
            // Profile permissions (admin needs these too)
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "profile.read.self");
            await AddClaimToRoleIfNotExists(roleManager, adminRole, "permission", "profile.update.self");
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
            dataType: "number",
            isVariant: true,
            description: "Storage capacity for electronics",
            unit: "GB",
            displayOrder: 6);

        await EnsureAttributeDefinitionAsync(dbContext,
            code: "ram",
            name: "RAM",
            dataType: "number",
            isVariant: true,
            description: "RAM capacity for electronics",
            unit: "GB",
            displayOrder: 7);

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

    // Demo data seeding methods
    public static async Task SeedDemoDataAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<RoleEntity> roleManager,
        IHostEnvironment env)
    {
        // Skip if demo data already exists (check for demo users or demo products)
        var hasDemoUsers = await dbContext.DomainUsers.AnyAsync(u => 
            u.Email != null && u.Email.StartsWith("demo"));
        var hasProducts = await dbContext.Products.AnyAsync();
        
        if (hasDemoUsers || hasProducts)
        {
            Console.WriteLine("⚠️ Demo data already exists, skipping seeding");
            return;
        }

        Console.WriteLine("🌱 Starting demo data seeding...");

        // 1. Seed categories
        await SeedCategoriesAsync(dbContext);

        // 2. Seed tags
        await SeedTagsAsync(dbContext);

        // 3. Seed demo users and stores
        await SeedDemoUsersAndStoresAsync(dbContext, userManager, roleManager);

        // 4. Seed catalog demo products
        await SeedCatalogDemoProductsAsync(dbContext);

        // 5. Seed demo products
        await SeedDemoProductsAsync(dbContext);

        Console.WriteLine("✅ Demo data seeding completed!");
    }

    private static async Task SeedCategoriesAsync(AppDbContext dbContext)
    {
        Console.WriteLine("📂 Seeding categories...");

        var categoriesToCreate = new[]
        {
            // Basic categories from catalog
            ("Electronics", "🔌", "Gadgets and devices", null),
            ("Accessories", "🔗", "Cables, cases, add-ons", null),
            ("Clothing", "👕", "Apparel and basics", null),

            // Electronics subcategories
            ("Smartphones", "📱", "Mobile phones and accessories", "Electronics"),
            ("Laptops", "💻", "Laptop computers", "Electronics"),
            ("Gaming", "🎮", "Gaming consoles and accessories", "Electronics"),

            // New main categories
            ("Home & Garden", "🏠", "Home improvement and garden supplies", null),
            ("Sports & Outdoors", "⚽", "Sports equipment and outdoor gear", null),
            ("Beauty & Personal Care", "💄", "Cosmetics, skincare, and personal care", null),
        };

        foreach (var (name, emoji, description, parentName) in categoriesToCreate)
        {
            var slug = SlugHelper.GenerateSlug(name);
            var existing = await dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            if (existing != null) continue;

            Guid? parentCategoryId = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                var parentCategory = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == parentName);
                parentCategoryId = parentCategory?.Id;
            }

            var category = Category.Create(name, $"{emoji} {description}", parentCategoryId);
            dbContext.Categories.Add(category);
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine("✅ Additional categories seeded successfully!");
    }

    private static async Task SeedTagsAsync(AppDbContext dbContext)
    {
        Console.WriteLine("🏷️ Seeding tags...");

        var tagsToCreate = new[]
        {
            ("New", "Recently added"),
            ("Popular", "Trending items"),
            ("Sale", "Discounted"),
            ("Wireless", "Wireless connectivity"),
            ("Gaming", "Perfect for gaming"),
            ("Eco-Friendly", "Environmentally friendly"),
            ("Premium", "High-quality premium products"),
        };

        foreach (var (name, description) in tagsToCreate)
        {
            var slug = SlugHelper.GenerateSlug(name);
            var existing = await dbContext.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (existing != null) continue;

            var tag = Tag.Create(name, description);
            dbContext.Tags.Add(tag);
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine("✅ Additional tags seeded successfully!");
    }

    private static async Task SeedDemoUsersAndStoresAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<RoleEntity> roleManager)
    {
        Console.WriteLine("👥 Creating demo users and stores...");

        // Create 3 demo users with secure passwords
        var demoUsers = new[]
        {
            ("demouser1@example.com", "Demo User 1", "Demo123!", false),
            ("demouser2@example.com", "Demo User 2", "Demo123!", false),
            ("demoseller1@example.com", "Demo Seller 1", "Demo123!", true),
        };

        foreach (var (email, name, password, isSeller) in demoUsers)
        {
            var existingIdentityUser = await userManager.FindByEmailAsync(email);
            var existingDomainUser = await dbContext.DomainUsers.FirstOrDefaultAsync(u => u.Email == email);

            if (existingIdentityUser != null || existingDomainUser != null)
            {
                Console.WriteLine($"User {email} already exists, skipping");
                continue;
            }

            try
            {
                var identityUser = new ApplicationUser
                {
                    UserName = email.Split('@')[0],
                    Email = email,
                    EmailConfirmed = true,
                    PhoneNumber = "+1234567890",
                    PhoneNumberConfirmed = true
                };

                var result = await userManager.CreateAsync(identityUser, password);
                if (!result.Succeeded)
                {
                    Console.WriteLine($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    continue;
                }

                await userManager.AddToRoleAsync(identityUser, isSeller ? Roles.Seller : Roles.User);

                var domainUser = new Domain.Entities.User(identityUser.Id, email);
                var nameParts = name.Split(' ');
                domainUser.UpdateProfile(nameParts[0], nameParts.Length > 1 ? nameParts[1] : "");
                dbContext.DomainUsers.Add(domainUser);

                if (isSeller)
                {
                    var store = Store.Create(domainUser.Id, $"{name}'s Store", $"Demo store for {name}");
                    store.Verify();
                    dbContext.Stores.Add(store);
                }

                await dbContext.SaveChangesAsync();
                Console.WriteLine($"✅ Created demo user: {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception creating user {email}: {ex.Message}");
            }
        }

        Console.WriteLine("✅ Demo users and stores seeding completed!");
    }

    private static async Task SeedDemoProductsAsync(AppDbContext dbContext)
    {
        Console.WriteLine("📦 Creating demo products...");

        var stores = await dbContext.Stores.Include(s => s.User).ToListAsync();
        if (!stores.Any())
        {
            Console.WriteLine("⚠️ No stores found, skipping product creation");
            return;
        }

        var categories = await dbContext.Categories.ToListAsync();
        var tags = await dbContext.Tags.ToListAsync();
        var attributeDefinitions = await dbContext.AttributeDefinitions.ToListAsync();

        var sampleProducts = new[]
        {
            ("iPhone 15 Pro", "Latest iPhone with advanced features", 1200m, "Smartphones"),
            ("Gaming Mouse RGB", "High-precision gaming mouse", 80m, "Gaming"),
            ("Yoga Mat Premium", "Non-slip exercise mat", 35m, "Sports & Outdoors"),
        };

        Dictionary<string, object?> GetRandomAttributes(string categoryName)
        {
            var random = new Random();
            switch (categoryName)
            {
                case "Smartphones":
                    return new Dictionary<string, object?>
                    {
                        ["color"] = new[] { "Black", "White", "Blue", "Gold" }[random.Next(4)],
                        ["storage"] = new[] { 128, 256, 512 }[random.Next(3)],
                        ["ram"] = new[] { 4, 8 }[random.Next(2)],
                        ["screen_size"] = random.Next(60, 71) / 10.0,
                        ["brand"] = "Apple",
                        ["warranty"] = new[] { "1 year", "2 years" }[random.Next(2)]
                    };
                case "Gaming":
                    return new Dictionary<string, object?>
                    {
                        ["color"] = new[] { "Black", "White", "RGB" }[random.Next(3)],
                        ["material"] = new[] { "Plastic", "Metal" }[random.Next(2)],
                        ["brand"] = new[] { "Razer", "Logitech", "Corsair" }[random.Next(3)],
                        ["warranty"] = new[] { "1 year", "2 years" }[random.Next(2)],
                        ["connector"] = "USB-A"
                    };
                case "Sports & Outdoors":
                    return new Dictionary<string, object?>
                    {
                        ["color"] = new[] { "Black", "Blue", "Green" }[random.Next(3)],
                        ["size"] = new[] { "Standard", "Large" }[random.Next(2)],
                        ["material"] = new[] { "Rubber", "Foam" }[random.Next(2)],
                        ["weight"] = random.Next(10, 51) / 10.0,
                        ["brand"] = new[] { "Nike", "Adidas", "Puma" }[random.Next(3)]
                    };
                default:
                    return new Dictionary<string, object?>();
            }
        }

        var productsCreated = 0;
        foreach (var store in stores)
        {
            foreach (var (name, description, price, categoryName) in sampleProducts)
            {
                var productName = $"{store.User?.Name ?? "Demo"}'s {name}";
                var exists = await dbContext.Products.AnyAsync(p => p.Name == productName && p.StoreId == store.Id);
                if (exists) continue;

                var product = new Product(productName, description);
                product.UpdateBaseImage($"https://picsum.photos/seed/{productName.Replace(" ", "-").ToLower()}/400/400");

                store.AddProduct(product);
                dbContext.Products.Add(product);

                var category = categories.FirstOrDefault(c => c.Name.Contains(categoryName));
                if (category != null)
                {
                    product.AddCategory(category);
                }

                var randomTags = tags.OrderBy(t => Guid.NewGuid()).Take(new Random().Next(1, 4)).ToList();
                foreach (var tag in randomTags)
                {
                    product.AddTag(tag);
                }

                var skuAttributes = GetRandomAttributes(categoryName);
                var sku = SkuEntity.Create(product.Id, price, new Random().Next(5, 51), skuAttributes);
                product.AddSku(sku);

                // Set typed attributes
                sku.SetTypedAttributes(skuAttributes, attributeDefinitions);

                // Add gallery images with short storage keys (max 32 chars)
                var shortKey = $"d{productsCreated}";
                var gallery = new[]
                {
                    ($"{shortKey}-1", $"{name} view 1"),
                    ($"{shortKey}-2", $"{name} view 2"),
                };
                foreach (var (storageKey, altText) in gallery)
                {
                    var media = await EnsureMediaImageAsync(dbContext, storageKey, "image/jpeg", 800, 600, altText);
                    product.AddGalleryItem(media);
                }

                productsCreated++;
            }
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine($"✅ Created {productsCreated} demo products across {stores.Count} stores!");
    }

    private static async Task SeedCatalogDemoProductsAsync(AppDbContext dbContext)
    {
        var adminUser = await dbContext.DomainUsers.FirstOrDefaultAsync(u => u.Email == AdminEmail);
        if (adminUser is null)
        {
            return;
        }

        var store = await dbContext.Stores.FirstOrDefaultAsync(s => s.UserId == adminUser.Id);
        if (store is null)
        {
            store = Store.Create(adminUser.Id, "Admin Store", "Seeded store for demo catalog");
            store.Verify();
            dbContext.Stores.Add(store);
            await dbContext.SaveChangesAsync();
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

            // ⭐ Convert JSONB attributes to typed attributes
            if (skuAttributes != null && skuAttributes.Count > 0)
            {
                var attributeDefinitions = await dbContext.AttributeDefinitions.ToListAsync();
                sku.SetTypedAttributes(skuAttributes, attributeDefinitions);
            }

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
            skuAttributes: new Dictionary<string, object?> { ["color"] = "Black", ["material"] = "silicone" },
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
            skuAttributes: new Dictionary<string, object?> { ["size"] = "M", ["color"] = "Gray" },
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
            skuAttributes: new Dictionary<string, object?> { ["connector"] = "USB-C", ["color"] = "Black" },
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

        await SeedAttributeDefinitionsAsync(dbContext);

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
                    // Assign Admin role
                    await userManager.AddToRoleAsync(admin, Roles.Admin);

                    var domainAdmin = new User(admin.Id, "Admin", "User", adminEmail);
                    userRepository.Add(domainAdmin);
                    admin.DomainUserId = domainAdmin.Id;
                    await userManager.UpdateAsync(admin);

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Console.WriteLine("✅ Admin user created and assigned Admin role");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error: {error.Code} - {error.Description}");
                    }
                    await transaction.RollbackAsync();
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        if (env.IsDevelopment() || env.IsEnvironment("Testing"))
        {
            await SeedDemoDataAsync(dbContext, userManager, roleManager, env);
        }
    }
}