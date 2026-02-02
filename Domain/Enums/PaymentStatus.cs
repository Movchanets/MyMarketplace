namespace Domain.Enums;

/// <summary>
/// Represents the payment status of an order
/// </summary>
public enum PaymentStatus
{
	/// <summary>
	/// Payment has not been initiated
	/// </summary>
	Pending = 0,

	/// <summary>
	/// Payment is being processed
	/// </summary>
	Processing = 1,

	/// <summary>
	/// Payment has been completed successfully
	/// </summary>
	Paid = 2,

	/// <summary>
	/// Payment has failed
	/// </summary>
	Failed = 3,

	/// <summary>
	/// Payment has been refunded
	/// </summary>
	Refunded = 4,

	/// <summary>
	/// Partial refund has been issued
	/// </summary>
	PartiallyRefunded = 5
}

/// <summary>
/// Extension methods for PaymentStatus enum
/// </summary>
public static class PaymentStatusExtensions
{
	/// <summary>
	/// Gets the display name for the payment status
	/// </summary>
	public static string GetDisplayName(this PaymentStatus status)
	{
		return status switch
		{
			PaymentStatus.Pending => "Pending",
			PaymentStatus.Processing => "Processing",
			PaymentStatus.Paid => "Paid",
			PaymentStatus.Failed => "Failed",
			PaymentStatus.Refunded => "Refunded",
			PaymentStatus.PartiallyRefunded => "Partially Refunded",
			_ => "Unknown"
		};
	}

	/// <summary>
	/// Checks if the payment status indicates successful payment
	/// </summary>
	public static bool IsPaid(this PaymentStatus status)
	{
		return status == PaymentStatus.Paid || status == PaymentStatus.Refunded || status == PaymentStatus.PartiallyRefunded;
	}

	/// <summary>
	/// Checks if the payment can be refunded
	/// </summary>
	public static bool CanRefund(this PaymentStatus status)
	{
		return status == PaymentStatus.Paid || status == PaymentStatus.PartiallyRefunded;
	}
}
