namespace Application.DTOs;

/// <summary>
/// DTO for favorite product display
/// </summary>
public record FavoriteProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? BaseImageUrl,
    decimal? MinPrice,
    bool InStock,
    DateTimeOffset FavoritedAt
);

/// <summary>
/// Request DTO for adding to favorites
/// </summary>
public record AddToFavoritesRequest(Guid ProductId);

/// <summary>
/// Request DTO for removing from favorites
/// </summary>
public record RemoveFromFavoritesRequest(Guid ProductId);

/// <summary>
/// Request DTO for merging guest favorites
/// </summary>
public record MergeGuestFavoritesRequest(IEnumerable<Guid> ProductIds);