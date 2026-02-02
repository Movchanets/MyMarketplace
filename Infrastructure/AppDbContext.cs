using System;
using Domain.Entities;
using Infrastructure.Configuration;
using Infrastructure.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<
    ApplicationUser,
    RoleEntity,
    Guid,
    IdentityUserClaim<Guid>,
    ApplicationUserRole,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>>(options)
{
    // Domain entities
    public DbSet<User> DomainUsers { get; set; }
    public DbSet<MediaImage> MediaImages { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ProductTag> ProductTags { get; set; }
    public DbSet<SkuEntity> Skus { get; set; }
    public DbSet<SkuAttributeValue> SkuAttributeValues { get; set; }
    public DbSet<ProductGallery> ProductGalleries { get; set; }
    public DbSet<SkuGallery> SkuGalleries { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
    public DbSet<SearchQuery> SearchQueries { get; set; }
    public DbSet<ProductFavorite> ProductFavorites { get; set; }

    // Cart and Order entities
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    // Stock reservation entities
    public DbSet<StockReservation> StockReservations { get; set; }

    // MassTransit EF Core Outbox/Inbox entities (for transactional message delivery)
    public DbSet<MassTransit.EntityFrameworkCoreIntegration.InboxState> InboxStates { get; set; }
    public DbSet<MassTransit.EntityFrameworkCoreIntegration.OutboxState> OutboxStates { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Ensure pgcrypto extension is available for gen_random_bytes function
        // This is required for RowVersion default values on Cart and Order entities
        builder.HasPostgresExtension("pgcrypto");

        // Apply configurations from assembly first
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure OutboxMessage entity (after assembly configurations to avoid conflicts)
        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(255);
            entity.Property(e => e.AggregateId).HasMaxLength(255);
            entity.Property(e => e.AggregateType).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.RetryCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ScheduledFor);
            entity.Property(e => e.ProcessedAt);
            entity.Property(e => e.ErrorMessage);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasIndex(e => e.CorrelationId);
        });

        // Configure MassTransit EF Core Inbox/Outbox entities
        builder.Entity<MassTransit.EntityFrameworkCoreIntegration.InboxState>(entity =>
        {
            entity.ToTable("InboxState");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageId).IsRequired();
            entity.Property(e => e.ConsumerId).IsRequired();
            entity.Property(e => e.LockId).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.MessageId, e.ConsumerId }).IsUnique();
            entity.HasIndex(e => e.Received);
        });

        builder.Entity<MassTransit.EntityFrameworkCoreIntegration.OutboxState>(entity =>
        {
            entity.ToTable("OutboxState");
            entity.HasKey(e => e.OutboxId);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.Created);
        });
        // Налаштування Domain User
        builder.Entity<User>(user =>
         {
             user.ToTable("DomainUsers");
             user.HasKey(u => u.Id);


             // Явно вказуємо використовувати послідовний Guid для продуктивності
             user.Property(u => u.Id)
                .ValueGeneratedOnAdd()
                .HasValueGenerator<SequentialGuidValueGenerator>(); //

             user.Property(u => u.Name).HasMaxLength(100);
             user.Property(u => u.Surname).HasMaxLength(100);
             user.Property(u => u.Email).HasMaxLength(100);
             user.Property(u => u.PhoneNumber).HasMaxLength(20);
             user.Property(u => u.IsBlocked).IsRequired();
             user.HasIndex(u => u.IdentityUserId).IsUnique();
             user.HasOne(u => u.Avatar)      // Юзер має одну картинку
                 .WithMany()                 // Картинка не обов'язково знає про Юзера (unidirectional)
                 .HasForeignKey(u => u.AvatarId)
                 .OnDelete(DeleteBehavior.SetNull);
         });

        // Налаштування ApplicationUser (Identity)
        builder.Entity<ApplicationUser>(appUser =>
        {
            appUser.ToTable("AspNetUsers");

            // --- КРИТИЧНЕ ВДОСКОНАЛЕННЯ ---
            appUser.Property(au => au.Id)
               .ValueGeneratedOnAdd()
               .HasValueGenerator<SequentialGuidValueGenerator>(); //

            // Зв'язок ApplicationUser -> DomainUser (один до одного)
            appUser.HasOne(au => au.DomainUser)
               .WithOne()
               .HasForeignKey<ApplicationUser>(au => au.DomainUserId)
                    .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);

            appUser.Property(au => au.RefreshToken).HasMaxLength(500);
        });

        // Налаштування ApplicationUserRole (many-to-many)
        builder.Entity<ApplicationUserRole>(userRole =>
        {
            userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

            userRole.HasOne(ur => ur.User)
               .WithMany(u => u.UserRoles)
               .HasForeignKey(ur => ur.UserId)
               .IsRequired();

            userRole.HasOne(ur => ur.Role)
               .WithMany(r => r.UserRoles)
               .HasForeignKey(ur => ur.RoleId)
               .IsRequired();
        });

        // Налаштування RoleEntity
        builder.Entity<RoleEntity>(role =>
        {
            // --- КРИТИЧНЕ ВДОСКОНАЛЕННЯ ---
            role.Property(r => r.Id)
               .ValueGeneratedOnAdd()
               .HasValueGenerator<SequentialGuidValueGenerator>(); //

            role.Property(r => r.Description).HasMaxLength(500);
        });

        // Cart, CartItem, Order, OrderItem, StockReservation configurations
        // are applied via IEntityTypeConfiguration classes in Configuration folder
    }
}