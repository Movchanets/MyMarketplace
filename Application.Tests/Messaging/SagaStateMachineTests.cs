using System.Diagnostics;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Messaging;

/// <summary>
/// Tests for saga state machines covering long-running transactional processes.
/// Tests the Checkout Saga that orchestrates multi-step atomic operations:
/// validate → reserve → charge → confirm
/// </summary>
public class SagaStateMachineTests : IAsyncLifetime
{
	private MassTransitTestHarness _harness = null!;
	private Mock<ILogger<CheckoutSaga>> _loggerMock = null!;

	// Saga state tracking
	private CheckoutSagaState _sagaState = null!;

	public async Task InitializeAsync()
	{
		_loggerMock = new Mock<ILogger<CheckoutSaga>>();
		_sagaState = new CheckoutSagaState();

		_harness = new MassTransitTestHarness(configureServices: services =>
		{
			services.AddSingleton(_loggerMock.Object);
			services.AddSingleton(_sagaState);
		});

		await _harness.InitializeAsync();
	}

	public async Task DisposeAsync()
	{
		await _harness.DisposeAsync();
	}

	[Fact]
	public async Task CheckoutSaga_WhenStarted_ShouldTransitionToValidatingState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var userId = Guid.NewGuid();

		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);
		var context = CreateSagaContext(correlationId, orderId, userId);

		// Act
		await saga.StartAsync(context);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Validating);
		_sagaState.CorrelationId.Should().Be(correlationId);
		_sagaState.OrderId.Should().Be(orderId);
	}

	[Fact]
	public async Task CheckoutSaga_WhenValidationSucceeds_ShouldTransitionToReservingState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Validating;
		_sagaState.CorrelationId = correlationId;

		var validationResult = new OrderValidationSucceeded
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			ValidatedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandleValidationSucceededAsync(validationResult);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Reserving);
		_loggerMock.VerifyLog(LogLevel.Information, "Validation succeeded", Moq.Times.Once());
	}

	[Fact]
	public async Task CheckoutSaga_WhenValidationFails_ShouldTransitionToFailedState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Validating;
		_sagaState.CorrelationId = correlationId;

		var validationFailed = new OrderValidationFailed
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			Reason = "Invalid cart items",
			FailedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandleValidationFailedAsync(validationFailed);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Failed);
		_sagaState.ErrorMessage.Should().Be("Invalid cart items");
	}

	[Fact]
	public async Task CheckoutSaga_WhenReservationSucceeds_ShouldTransitionToChargingState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Reserving;
		_sagaState.CorrelationId = correlationId;

		var reservationResult = new StockReserved
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			ReservationId = Guid.NewGuid(),
			ReservedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandleStockReservedAsync(reservationResult);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Charging);
		_sagaState.ReservationId.Should().Be(reservationResult.ReservationId);
	}

	[Fact]
	public async Task CheckoutSaga_WhenReservationFails_ShouldTransitionToFailedState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Reserving;
		_sagaState.CorrelationId = correlationId;

		var reservationFailed = new StockReservationFailed
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			Reason = "Insufficient stock",
			FailedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandleStockReservationFailedAsync(reservationFailed);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Failed);
		_sagaState.ErrorMessage.Should().Be("Insufficient stock");
	}

	[Fact]
	public async Task CheckoutSaga_WhenPaymentSucceeds_ShouldTransitionToConfirmingState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Charging;
		_sagaState.CorrelationId = correlationId;
		_sagaState.ReservationId = Guid.NewGuid();

		var paymentResult = new PaymentSucceeded
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			TransactionId = "txn_12345",
			ChargedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandlePaymentSucceededAsync(paymentResult);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Confirming);
		_sagaState.TransactionId.Should().Be("txn_12345");
	}

	[Fact]
	public async Task CheckoutSaga_WhenPaymentFails_ShouldTransitionToCompensatingState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Charging;
		_sagaState.CorrelationId = correlationId;
		_sagaState.ReservationId = Guid.NewGuid();

		var paymentFailed = new PaymentFailed
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			Reason = "Card declined",
			FailedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandlePaymentFailedAsync(paymentFailed);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Compensating);
		_sagaState.ErrorMessage.Should().Be("Card declined");
	}

	[Fact]
	public async Task CheckoutSaga_WhenConfirmationSucceeds_ShouldTransitionToCompletedState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Confirming;
		_sagaState.CorrelationId = correlationId;
		_sagaState.TransactionId = "txn_12345";

		var confirmationResult = new OrderConfirmed
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			ConfirmedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandleOrderConfirmedAsync(confirmationResult);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Completed);
		_sagaState.CompletedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task CheckoutSaga_WhenCompensationCompletes_ShouldTransitionToFailedState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Compensating;
		_sagaState.CorrelationId = correlationId;
		_sagaState.ReservationId = Guid.NewGuid();

		var compensationResult = new StockReservationReleased
		{
			CorrelationId = correlationId,
			OrderId = Guid.NewGuid(),
			ReleasedAt = DateTime.UtcNow
		};

		// Act
		await saga.HandleCompensationCompletedAsync(compensationResult);

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Failed);
	}

	[Fact]
	public async Task CheckoutSaga_TimeoutDuringValidation_ShouldTransitionToFailedState()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		_sagaState.CurrentState = CheckoutState.Validating;
		_sagaState.CorrelationId = correlationId;
		_sagaState.StartedAt = DateTime.UtcNow.AddMinutes(-5); // Started 5 minutes ago

		// Act
		await saga.HandleTimeoutAsync();

		// Assert
		_sagaState.CurrentState.Should().Be(CheckoutState.Failed);
		_sagaState.ErrorMessage.Should().Contain("timed out");
	}

	[Fact]
	public async Task CheckoutSaga_FullHappyPath_ShouldCompleteSuccessfully()
	{
		// Arrange
		var correlationId = Guid.NewGuid();
		var orderId = Guid.NewGuid();
		var saga = new CheckoutSaga(_loggerMock.Object, _sagaState);

		// Step 1: Start
		await saga.StartAsync(CreateSagaContext(correlationId, orderId, Guid.NewGuid()));
		_sagaState.CurrentState.Should().Be(CheckoutState.Validating);

		// Step 2: Validation Success
		await saga.HandleValidationSucceededAsync(new OrderValidationSucceeded
		{
			CorrelationId = correlationId,
			OrderId = orderId,
			ValidatedAt = DateTime.UtcNow
		});
		_sagaState.CurrentState.Should().Be(CheckoutState.Reserving);

		// Step 3: Reservation Success
		await saga.HandleStockReservedAsync(new StockReserved
		{
			CorrelationId = correlationId,
			OrderId = orderId,
			ReservationId = Guid.NewGuid(),
			ReservedAt = DateTime.UtcNow
		});
		_sagaState.CurrentState.Should().Be(CheckoutState.Charging);

		// Step 4: Payment Success
		await saga.HandlePaymentSucceededAsync(new PaymentSucceeded
		{
			CorrelationId = correlationId,
			OrderId = orderId,
			TransactionId = "txn_12345",
			ChargedAt = DateTime.UtcNow
		});
		_sagaState.CurrentState.Should().Be(CheckoutState.Confirming);

		// Step 5: Confirmation Success
		await saga.HandleOrderConfirmedAsync(new OrderConfirmed
		{
			CorrelationId = correlationId,
			OrderId = orderId,
			ConfirmedAt = DateTime.UtcNow
		});
		_sagaState.CurrentState.Should().Be(CheckoutState.Completed);
	}

	private static CheckoutSagaContext CreateSagaContext(Guid correlationId, Guid orderId, Guid userId)
	{
		return new CheckoutSagaContext
		{
			CorrelationId = correlationId,
			OrderId = orderId,
			UserId = userId,
			CartId = Guid.NewGuid(),
			StartedAt = DateTime.UtcNow
		};
	}
}

/// <summary>
/// Saga state machine implementation for testing.
/// </summary>
public class CheckoutSaga
{
	private readonly ILogger<CheckoutSaga> _logger;
	private readonly CheckoutSagaState _state;

	public CheckoutSaga(ILogger<CheckoutSaga> logger, CheckoutSagaState state)
	{
		_logger = logger;
		_state = state;
	}

	public async Task StartAsync(CheckoutSagaContext context)
	{
		_logger.LogInformation("Starting checkout saga for order {OrderId}", context.OrderId);

		_state.CorrelationId = context.CorrelationId;
		_state.OrderId = context.OrderId;
		_state.UserId = context.UserId;
		_state.CartId = context.CartId;
		_state.StartedAt = context.StartedAt;
		_state.CurrentState = CheckoutState.Validating;

		// Publish validation command
		await Task.CompletedTask;
	}

	public async Task HandleValidationSucceededAsync(OrderValidationSucceeded message)
	{
		_logger.LogInformation("Validation succeeded for order {OrderId}", message.OrderId);
		_state.CurrentState = CheckoutState.Reserving;
		await Task.CompletedTask;
	}

	public async Task HandleValidationFailedAsync(OrderValidationFailed message)
	{
		_logger.LogWarning("Validation failed for order {OrderId}: {Reason}", message.OrderId, message.Reason);
		_state.CurrentState = CheckoutState.Failed;
		_state.ErrorMessage = message.Reason;
		await Task.CompletedTask;
	}

	public async Task HandleStockReservedAsync(StockReserved message)
	{
		_logger.LogInformation("Stock reserved for order {OrderId}", message.OrderId);
		_state.ReservationId = message.ReservationId;
		_state.CurrentState = CheckoutState.Charging;
		await Task.CompletedTask;
	}

	public async Task HandleStockReservationFailedAsync(StockReservationFailed message)
	{
		_logger.LogWarning("Stock reservation failed for order {OrderId}: {Reason}", message.OrderId, message.Reason);
		_state.CurrentState = CheckoutState.Failed;
		_state.ErrorMessage = message.Reason;
		await Task.CompletedTask;
	}

	public async Task HandlePaymentSucceededAsync(PaymentSucceeded message)
	{
		_logger.LogInformation("Payment succeeded for order {OrderId}", message.OrderId);
		_state.TransactionId = message.TransactionId;
		_state.CurrentState = CheckoutState.Confirming;
		await Task.CompletedTask;
	}

	public async Task HandlePaymentFailedAsync(PaymentFailed message)
	{
		_logger.LogWarning("Payment failed for order {OrderId}: {Reason}", message.OrderId, message.Reason);
		_state.CurrentState = CheckoutState.Compensating;
		_state.ErrorMessage = message.Reason;
		await Task.CompletedTask;
	}

	public async Task HandleOrderConfirmedAsync(OrderConfirmed message)
	{
		_logger.LogInformation("Order {OrderId} confirmed", message.OrderId);
		_state.CurrentState = CheckoutState.Completed;
		_state.CompletedAt = DateTime.UtcNow;
		await Task.CompletedTask;
	}

	public async Task HandleCompensationCompletedAsync(StockReservationReleased message)
	{
		_logger.LogInformation("Compensation completed for order {OrderId}", message.OrderId);
		_state.CurrentState = CheckoutState.Failed;
		await Task.CompletedTask;
	}

	public async Task HandleTimeoutAsync()
	{
		_logger.LogError("Checkout saga timed out for correlation {CorrelationId}", _state.CorrelationId);
		_state.CurrentState = CheckoutState.Failed;
		_state.ErrorMessage = "Operation timed out";
		await Task.CompletedTask;
	}
}

/// <summary>
/// Saga state tracking.
/// </summary>
public class CheckoutSagaState
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public Guid UserId { get; set; }
	public Guid CartId { get; set; }
	public Guid? ReservationId { get; set; }
	public string? TransactionId { get; set; }
	public CheckoutState CurrentState { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime StartedAt { get; set; }
	public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Checkout states.
/// </summary>
public enum CheckoutState
{
	Initial,
	Validating,
	Reserving,
	Charging,
	Confirming,
	Completed,
	Compensating,
	Failed
}

/// <summary>
/// Saga context for starting checkout.
/// </summary>
public class CheckoutSagaContext
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public Guid UserId { get; set; }
	public Guid CartId { get; set; }
	public DateTime StartedAt { get; set; }
}

/// <summary>
/// Saga events.
/// </summary>
public class OrderValidationSucceeded
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public DateTime ValidatedAt { get; set; }
}

public class OrderValidationFailed
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public string Reason { get; set; } = string.Empty;
	public DateTime FailedAt { get; set; }
}

public class StockReserved
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public Guid ReservationId { get; set; }
	public DateTime ReservedAt { get; set; }
}

public class StockReservationFailed
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public string Reason { get; set; } = string.Empty;
	public DateTime FailedAt { get; set; }
}

public class PaymentSucceeded
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public string TransactionId { get; set; } = string.Empty;
	public DateTime ChargedAt { get; set; }
}

public class PaymentFailed
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public string Reason { get; set; } = string.Empty;
	public DateTime FailedAt { get; set; }
}

public class OrderConfirmed
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public DateTime ConfirmedAt { get; set; }
}

public class StockReservationReleased
{
	public Guid CorrelationId { get; set; }
	public Guid OrderId { get; set; }
	public DateTime ReleasedAt { get; set; }
}

/// <summary>
/// Logger mock extensions.
/// </summary>
public static class LoggerMockExtensions
{
	public static void VerifyLog(this Mock<ILogger<CheckoutSaga>> logger, LogLevel level, string message, Times times)
	{
		logger.Verify(
			x => x.Log(
				level,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
				It.IsAny<Exception?>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
			times);
	}
}
