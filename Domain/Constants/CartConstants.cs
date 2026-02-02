namespace Domain.Constants;

/// <summary>
/// Constants related to cart operations and constraints
/// </summary>
public static class CartConstants
{
	/// <summary>
	/// Minimum quantity allowed per cart item
	/// </summary>
	public const int MinQuantityPerItem = 1;

	/// <summary>
	/// Maximum quantity allowed per SKU in cart
	/// </summary>
	public const int MaxQuantityPerSku = 99;


}
