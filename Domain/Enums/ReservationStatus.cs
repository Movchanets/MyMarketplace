namespace Domain.Enums;

/// <summary>
/// Represents the current status of a stock reservation
/// </summary>
public enum ReservationStatus
{
	/// <summary>
	/// Reservation is active and holding stock
	/// </summary>
	Active = 0,

	/// <summary>
	/// Reservation has been converted to an actual order deduction
	/// </summary>
	Converted = 1,

	/// <summary>
	/// Reservation has expired and stock has been released
	/// </summary>
	Expired = 2,

	/// <summary>
	/// Reservation was manually cancelled
	/// </summary>
	Cancelled = 3
}

/// <summary>
/// Extension methods for ReservationStatus enum
/// </summary>
public static class ReservationStatusExtensions
{
	/// <summary>
	/// Gets the display name for the reservation status
	/// </summary>
	public static string GetDisplayName(this ReservationStatus status)
	{
		return status switch
		{
			ReservationStatus.Active => "Active",
			ReservationStatus.Converted => "Converted",
			ReservationStatus.Expired => "Expired",
			ReservationStatus.Cancelled => "Cancelled",
			_ => "Unknown"
		};
	}

	/// <summary>
	/// Checks if the reservation is still holding stock
	/// </summary>
	public static bool IsHoldingStock(this ReservationStatus status)
	{
		return status == ReservationStatus.Active;
	}

	/// <summary>
	/// Checks if the reservation can be cancelled
	/// </summary>
	public static bool CanCancel(this ReservationStatus status)
	{
		return status == ReservationStatus.Active;
	}

	/// <summary>
	/// Checks if the reservation can be converted to an order
	/// </summary>
	public static bool CanConvert(this ReservationStatus status)
	{
		return status == ReservationStatus.Active;
	}
}
