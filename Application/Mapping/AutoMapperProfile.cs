using Application.DTOs;
using AutoMapper;
using Application.Models;
using Domain.Entities;
using System.Text.Json;

namespace Application.Mapping;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Map from Domain User to UserVM (identity-specific fields left default)
        CreateMap<User, UserVM>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.IdentityUserId))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Surname ?? string.Empty))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src =>
                src.Avatar != null ? src.Avatar.StorageKey : string.Empty
            ))
            .ForMember(dest => dest.IsBlocked, opt => opt.MapFrom(src => src.IsBlocked));

        // Map from Domain User to UserDto
        // This mapping uses context items for identity-specific fields (Username, Email, PhoneNumber, Roles, AvatarUrl)
        // Since UserDto is a record, we use constructor parameters with context items
        CreateMap<User, UserDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.IdentityUserId))
            .ForCtorParam("Username", opt => opt.MapFrom((src, ctx) =>
                ctx.Items.TryGetValue("Username", out var username) ? username as string ?? string.Empty : string.Empty))
            .ForCtorParam("Name", opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForCtorParam("Surname", opt => opt.MapFrom(src => src.Surname ?? string.Empty))
            .ForCtorParam("Email", opt => opt.MapFrom((src, ctx) =>
                ctx.Items.TryGetValue("Email", out var email) ? email as string ?? string.Empty : string.Empty))
            .ForCtorParam("PhoneNumber", opt => opt.MapFrom((src, ctx) =>
                ctx.Items.TryGetValue("PhoneNumber", out var phone) ? phone as string ?? string.Empty : string.Empty))
            .ForCtorParam("Roles", opt => opt.MapFrom((src, ctx) =>
                ctx.Items.TryGetValue("Roles", out var roles) ? roles as List<string> ?? new List<string>() : new List<string>()))
            .ForCtorParam("AvatarUrl", opt => opt.MapFrom((src, ctx) =>
                ctx.Items.TryGetValue("AvatarUrl", out var avatarUrl) ? avatarUrl as string : null));

        // Mapping from UserDto to Domain User is not direct; handlers should coordinate domain + identity updates.

        // Catalog DTO mappings
        CreateMap<Store, StoreSummaryDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("Name", opt => opt.MapFrom(src => src.Name))
            .ForCtorParam("Slug", opt => opt.MapFrom(src => src.Slug))
            .ForCtorParam("Description", opt => opt.MapFrom(src => src.Description))
            .ForCtorParam("IsVerified", opt => opt.MapFrom(src => src.IsVerified));

        CreateMap<Store, StoreDetailsDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("Name", opt => opt.MapFrom(src => src.Name))
            .ForCtorParam("Slug", opt => opt.MapFrom(src => src.Slug))
            .ForCtorParam("Description", opt => opt.MapFrom(src => src.Description))
            .ForCtorParam("IsVerified", opt => opt.MapFrom(src => src.IsVerified))
            .ForCtorParam("IsSuspended", opt => opt.MapFrom(src => src.IsSuspended));

        CreateMap<Category, CategoryDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("Name", opt => opt.MapFrom(src => src.Name))
            .ForCtorParam("Slug", opt => opt.MapFrom(src => src.Slug))
            .ForCtorParam("Description", opt => opt.MapFrom(src => src.Description))
            .ForCtorParam("ParentCategoryId", opt => opt.MapFrom(src => src.ParentCategoryId));

        CreateMap<Tag, TagDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("Name", opt => opt.MapFrom(src => src.Name))
            .ForCtorParam("Slug", opt => opt.MapFrom(src => src.Slug))
            .ForCtorParam("Description", opt => opt.MapFrom(src => src.Description));

        CreateMap<SkuEntity, SkuDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("SkuCode", opt => opt.MapFrom(src => src.SkuCode))
            .ForCtorParam("Price", opt => opt.MapFrom(src => src.Price))
            .ForCtorParam("StockQuantity", opt => opt.MapFrom(src => src.StockQuantity))
            .ForCtorParam("Attributes", opt => opt.MapFrom(src => DeserializeAttributes(src.Attributes)))
            .ForCtorParam("MergedAttributes", opt => opt.MapFrom(src => (Dictionary<string, object?>?)null));

        CreateMap<Product, ProductSummaryDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("StoreId", opt => opt.MapFrom(src => src.StoreId))
            .ForCtorParam("Name", opt => opt.MapFrom(src => src.Name))
            .ForCtorParam("BaseImageUrl", opt => opt.MapFrom(src => src.BaseImageUrl))
            .ForCtorParam("MinPrice", opt => opt.MapFrom(src => src.Skus.Count == 0 ? (decimal?)null : src.Skus.Min(s => s.Price)))
            .ForCtorParam("InStock", opt => opt.MapFrom(src => src.Skus.Any(s => s.StockQuantity > 0)))
            .ForCtorParam("Categories", opt => opt.MapFrom(src => src.ProductCategories
                .Where(pc => pc.Category != null)
                .Select(pc => pc.Category!)
                .ToList()))
            .ForCtorParam("Tags", opt => opt.MapFrom(src => src.ProductTags
                .Where(pt => pt.Tag != null)
                .Select(pt => pt.Tag!)
                .ToList()));
    }

    private static Dictionary<string, object?>? DeserializeAttributes(JsonDocument? attributes)
    {
        if (attributes is null)
        {
            return null;
        }

        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(attributes.RootElement.GetRawText());
        return dictionary;
    }
}