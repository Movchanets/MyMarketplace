namespace Domain.Entities;

/// <summary>
/// Represents a user's favorite product relationship
/// Uses composite primary key (UserId, ProductId) to prevent duplicates
/// </summary>
public class ProductFavorite
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}