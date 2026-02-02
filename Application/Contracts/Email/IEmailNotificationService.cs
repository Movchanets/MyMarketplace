namespace Application.Contracts.Email;

/// <summary>
/// Service interface for publishing email notification commands.
/// Defined in Application layer to maintain Clean Architecture boundaries.
/// Infrastructure layer provides the MassTransit-based implementation.
/// </summary>
public interface IEmailNotificationService
{
	/// <summary>
	/// Publishes an email notification command to the message bus
	/// </summary>
	/// <param name="command">The email command to publish</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the publish operation</returns>
	Task SendEmailAsync(ISendEmailCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a templated email notification
	/// </summary>
	/// <param name="templateName">Name of the email template</param>
	/// <param name="to">Recipient email address</param>
	/// <param name="templateData">Data to populate the template</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the publish operation</returns>
	Task SendTemplatedEmailAsync(string templateName, string to, object templateData, CancellationToken cancellationToken = default);

	/// <summary>
	/// Schedules an email to be sent at a specific time
	/// </summary>
	/// <param name="command">The email command</param>
	/// <param name="scheduledTime">When to send the email</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the schedule operation</returns>
	Task ScheduleEmailAsync(ISendEmailCommand command, DateTime scheduledTime, CancellationToken cancellationToken = default);
}

/// <summary>
/// Predefined email template names
/// </summary>
public static class EmailTemplates
{
	public const string OrderConfirmation = "OrderConfirmation";
	public const string OrderShipped = "OrderShipped";
	public const string OrderDelivered = "OrderDelivered";
	public const string OrderCancelled = "OrderCancelled";
	public const string PaymentReceived = "PaymentReceived";
	public const string PaymentFailed = "PaymentFailed";
	public const string Welcome = "Welcome";
	public const string PasswordReset = "PasswordReset";
	public const string CartReminder = "CartReminder";
	public const string CartAbandoned = "CartAbandoned";
}
