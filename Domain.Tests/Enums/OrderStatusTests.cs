using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Enums;

public class OrderStatusTests
{
	[Theory]
	[InlineData(OrderStatus.Pending, "Pending")]
	[InlineData(OrderStatus.Confirmed, "Confirmed")]
	[InlineData(OrderStatus.Processing, "Processing")]
	[InlineData(OrderStatus.Shipped, "Shipped")]
	[InlineData(OrderStatus.Delivered, "Delivered")]
	[InlineData(OrderStatus.Cancelled, "Cancelled")]
	public void GetDisplayName_ReturnsCorrectName(OrderStatus status, string expectedName)
	{
		// Act
		var displayName = status.GetDisplayName();

		// Assert
		displayName.Should().Be(expectedName);
	}

	[Theory]
	[InlineData(OrderStatus.Pending, true)]
	[InlineData(OrderStatus.Confirmed, true)]
	[InlineData(OrderStatus.Processing, true)]
	[InlineData(OrderStatus.Shipped, false)]
	[InlineData(OrderStatus.Delivered, false)]
	[InlineData(OrderStatus.Cancelled, false)]
	public void CanCancel_ReturnsCorrectValue(OrderStatus status, bool expected)
	{
		// Act & Assert
		status.CanCancel().Should().Be(expected);
	}

	[Theory]
	[InlineData(OrderStatus.Pending, true)]
	[InlineData(OrderStatus.Confirmed, true)]
	[InlineData(OrderStatus.Processing, true)]
	[InlineData(OrderStatus.Shipped, true)]
	[InlineData(OrderStatus.Delivered, false)]
	[InlineData(OrderStatus.Cancelled, false)]
	public void CanUpdateStatus_ReturnsCorrectValue(OrderStatus status, bool expected)
	{
		// Act & Assert
		status.CanUpdateStatus().Should().Be(expected);
	}

	[Theory]
	[InlineData(OrderStatus.Pending, OrderStatus.Confirmed, true)]
	[InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
	[InlineData(OrderStatus.Pending, OrderStatus.Shipped, false)]
	[InlineData(OrderStatus.Confirmed, OrderStatus.Processing, true)]
	[InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled, true)]
	[InlineData(OrderStatus.Confirmed, OrderStatus.Delivered, false)]
	[InlineData(OrderStatus.Processing, OrderStatus.Shipped, true)]
	[InlineData(OrderStatus.Processing, OrderStatus.Cancelled, true)]
	[InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
	[InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
	[InlineData(OrderStatus.Delivered, OrderStatus.Shipped, false)]
	[InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
	public void IsValidTransition_ReturnsCorrectValue(OrderStatus currentStatus, OrderStatus newStatus, bool expected)
	{
		// Act & Assert
		currentStatus.IsValidTransition(newStatus).Should().Be(expected);
	}

	[Fact]
	public void GetValidNextStatuses_Pending_ReturnsConfirmedAndCancelled()
	{
		// Act
		var nextStatuses = OrderStatus.Pending.GetValidNextStatuses().ToList();

		// Assert
		nextStatuses.Should().Contain(OrderStatus.Confirmed);
		nextStatuses.Should().Contain(OrderStatus.Cancelled);
		nextStatuses.Should().HaveCount(2);
	}

	[Fact]
	public void GetValidNextStatuses_Confirmed_ReturnsProcessingAndCancelled()
	{
		// Act
		var nextStatuses = OrderStatus.Confirmed.GetValidNextStatuses().ToList();

		// Assert
		nextStatuses.Should().Contain(OrderStatus.Processing);
		nextStatuses.Should().Contain(OrderStatus.Cancelled);
		nextStatuses.Should().HaveCount(2);
	}

	[Fact]
	public void GetValidNextStatuses_Processing_ReturnsShippedAndCancelled()
	{
		// Act
		var nextStatuses = OrderStatus.Processing.GetValidNextStatuses().ToList();

		// Assert
		nextStatuses.Should().Contain(OrderStatus.Shipped);
		nextStatuses.Should().Contain(OrderStatus.Cancelled);
		nextStatuses.Should().HaveCount(2);
	}

	[Fact]
	public void GetValidNextStatuses_Shipped_ReturnsDelivered()
	{
		// Act
		var nextStatuses = OrderStatus.Shipped.GetValidNextStatuses().ToList();

		// Assert
		nextStatuses.Should().ContainSingle();
		nextStatuses.Should().Contain(OrderStatus.Delivered);
	}

	[Fact]
	public void GetValidNextStatuses_Delivered_ReturnsEmpty()
	{
		// Act
		var nextStatuses = OrderStatus.Delivered.GetValidNextStatuses().ToList();

		// Assert
		nextStatuses.Should().BeEmpty();
	}

	[Fact]
	public void GetValidNextStatuses_Cancelled_ReturnsEmpty()
	{
		// Act
		var nextStatuses = OrderStatus.Cancelled.GetValidNextStatuses().ToList();

		// Assert
		nextStatuses.Should().BeEmpty();
	}
}
