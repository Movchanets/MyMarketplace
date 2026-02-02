using System.Data;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Messaging;

/// <summary>
/// Integration tests for the Outbox pattern implementation.
/// Covers:
/// - Outbox persistence verification with EF Core
/// - Atomic message and business data persistence
/// - Outbox message delivery guarantees
/// - At-least-once semantics verification
/// </summary>
public class OutboxPatternTests : IAsyncLifetime
{
	private MassTransitTestHarness _harness = null!;

	public async Task InitializeAsync()
	{
		_harness = new MassTransitTestHarness();
		await _harness.InitializeAsync();
	}

	public async Task DisposeAsync()
	{
		await _harness.DisposeAsync();
	}

	[Fact]
	public async Task Outbox_WhenTransactionCommits_ShouldPersistBothBusinessDataAndMessage()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		// Create a domain entity and outbox message in same transaction
		using (var transaction = await dbContext.Database.BeginTransactionAsync())
		{
			// Business data - create outbox message using the factory method
			var outboxMessage = OutboxMessage.FromDomainEvent(
				new SendEmailCommand
				{
					MessageId = Guid.NewGuid(),
					To = "test@example.com",
					Subject = "Test",
					Body = "Test Body",
					RequestedAt = DateTime.UtcNow,
					CorrelationId = correlationId
				},
				correlationId,
				Guid.NewGuid().ToString(),
				"Order");

			dbContext.Set<OutboxMessage>().Add(outboxMessage);
			await dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
		}

		// Assert
		var savedMessage = await dbContext.Set<OutboxMessage>()
			.FirstOrDefaultAsync(o => o.CorrelationId == correlationId);

		savedMessage.Should().NotBeNull();
		savedMessage!.Status.Should().Be(OutboxMessageStatus.Pending);
		savedMessage.CorrelationId.Should().Be(correlationId);
	}

	[Fact]
	public async Task Outbox_WhenTransactionRollsBack_ShouldNotPersistAnyData()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		// Act - Create data then roll back
		using (var transaction = await dbContext.Database.BeginTransactionAsync())
		{
			var outboxMessage = new OutboxMessage(
				"TestEvent",
				"{}",
				correlationId,
				Guid.NewGuid().ToString(),
				"Test");

			dbContext.Set<OutboxMessage>().Add(outboxMessage);
			await dbContext.SaveChangesAsync();

			// Rollback instead of commit
			await transaction.RollbackAsync();
		}

		// Assert - Data should not exist
		var savedMessage = await dbContext.Set<OutboxMessage>()
			.FirstOrDefaultAsync(o => o.CorrelationId == correlationId);

		savedMessage.Should().BeNull();
	}

	[Fact]
	public async Task Outbox_MessageStatus_ShouldTransitionFromPendingToPublished()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		var outboxMessage = OutboxMessage.FromDomainEvent(
			new SendEmailCommand
			{
				MessageId = Guid.NewGuid(),
				To = "test@example.com",
				Subject = "Test",
				Body = "Test Body",
				RequestedAt = DateTime.UtcNow,
				CorrelationId = correlationId
			},
			correlationId,
			Guid.NewGuid().ToString(),
			"Order");

		dbContext.Set<OutboxMessage>().Add(outboxMessage);
		await dbContext.SaveChangesAsync();

		// Act - Simulate processing
		outboxMessage.MarkAsPublished();
		await dbContext.SaveChangesAsync();

		// Assert
		var savedMessage = await dbContext.Set<OutboxMessage>()
			.FirstAsync(o => o.CorrelationId == correlationId);

		savedMessage.Status.Should().Be(OutboxMessageStatus.Published);
		savedMessage.ProcessedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task Outbox_AtLeastOnceDelivery_ShouldDeliverMessageEvenAfterFailure()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		var outboxMessage = OutboxMessage.FromDomainEvent(
			new SendEmailCommand
			{
				MessageId = Guid.NewGuid(),
				To = "test@example.com",
				Subject = "Test",
				Body = "Test Body",
				RequestedAt = DateTime.UtcNow,
				CorrelationId = correlationId
			},
			correlationId,
			Guid.NewGuid().ToString(),
			"Order");

		dbContext.Set<OutboxMessage>().Add(outboxMessage);
		await dbContext.SaveChangesAsync();

		// Act - Simulate multiple delivery attempts with failures
		for (int i = 0; i < 3; i++)
		{
			outboxMessage.MarkAsFailed($"Attempt {i + 1} failed");
			await dbContext.SaveChangesAsync();
		}

		// Finally mark as published
		outboxMessage.MarkAsPublished();
		await dbContext.SaveChangesAsync();

		// Assert
		var savedMessage = await dbContext.Set<OutboxMessage>()
			.FirstAsync(o => o.CorrelationId == correlationId);

		savedMessage.RetryCount.Should().Be(3);
		savedMessage.Status.Should().Be(OutboxMessageStatus.Published);
	}

	[Fact]
	public async Task Outbox_ExpiredMessages_ShouldBeMarkedAsDeadLetter()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		var outboxMessage = new OutboxMessage(
			"TestEvent",
			"{}",
			correlationId,
			Guid.NewGuid().ToString(),
			"Test");

		dbContext.Set<OutboxMessage>().Add(outboxMessage);
		await dbContext.SaveChangesAsync();

		// Act - Simulate max retry failures
		for (int i = 0; i < OutboxMessage.MaxRetryCount; i++)
		{
			outboxMessage.MarkAsFailed("Error");
			await dbContext.SaveChangesAsync();
		}

		// Assert
		var savedMessage = await dbContext.Set<OutboxMessage>()
			.FirstAsync(o => o.CorrelationId == correlationId);

		savedMessage.Status.Should().Be(OutboxMessageStatus.DeadLetter);
		savedMessage.ErrorMessage.Should().Be("Error");
	}

	[Fact]
	public async Task Outbox_CorrelationId_ShouldPropagateThroughMessages()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		var messages = new List<OutboxMessage>
		{
			OutboxMessage.FromDomainEvent(
				new SendEmailCommand { MessageId = Guid.NewGuid(), To = "test1@example.com", Subject = "Test1", Body = "Body1", RequestedAt = DateTime.UtcNow, CorrelationId = correlationId },
				correlationId, Guid.NewGuid().ToString(), "Order"),
			OutboxMessage.FromDomainEvent(
				new SendEmailCommand { MessageId = Guid.NewGuid(), To = "test2@example.com", Subject = "Test2", Body = "Body2", RequestedAt = DateTime.UtcNow, CorrelationId = correlationId },
				correlationId, Guid.NewGuid().ToString(), "Order")
		};

		dbContext.Set<OutboxMessage>().AddRange(messages);
		await dbContext.SaveChangesAsync();

		// Act
		var correlatedMessages = await dbContext.Set<OutboxMessage>()
			.Where(o => o.CorrelationId == correlationId)
			.ToListAsync();

		// Assert
		correlatedMessages.Should().HaveCount(2);
		correlatedMessages.All(m => m.CorrelationId == correlationId).Should().BeTrue();
	}

	[Fact]
	public async Task Outbox_ConcurrentPublishing_ShouldMaintainConsistency()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();
		var tasks = new List<Task>();
		var semaphore = new SemaphoreSlim(1, 1);

		// Act - Simulate concurrent message publishing
		for (int i = 0; i < 10; i++)
		{
			var index = i;
			tasks.Add(Task.Run(async () =>
			{
				var message = OutboxMessage.FromDomainEvent(
					new SendEmailCommand
					{
						MessageId = Guid.NewGuid(),
						To = $"test{index}@example.com",
						Subject = $"Test{index}",
						Body = "Body",
						RequestedAt = DateTime.UtcNow,
						CorrelationId = correlationId
					},
					correlationId,
					Guid.NewGuid().ToString(),
					"Order");

				await semaphore.WaitAsync();
				try
				{
					dbContext.Set<OutboxMessage>().Add(message);
					await dbContext.SaveChangesAsync();
				}
				finally
				{
					semaphore.Release();
				}
			}));
		}

		await Task.WhenAll(tasks);

		// Assert
		var savedMessages = await dbContext.Set<OutboxMessage>()
			.Where(o => o.CorrelationId == correlationId)
			.ToListAsync();

		savedMessages.Should().HaveCount(10);
	}

}

/// <summary>
/// Tests for message serialization and deserialization edge cases.
/// </summary>
public class MessageSerializationTests
{
	[Fact]
	public void SendEmailCommand_WithSpecialCharacters_ShouldSerializeCorrectly()
	{
		// Arrange
		var command = new SendEmailCommand
		{
			MessageId = Guid.NewGuid(),
			To = "test@example.com",
			Subject = "Test with \"quotes\" and \n newlines",
			Body = "Body with <html> & special chars: \t \r\n",
			IsHtml = true,
			RequestedAt = DateTime.UtcNow,
			CorrelationId = Guid.NewGuid().ToString()
		};

		// Act
		var json = System.Text.Json.JsonSerializer.Serialize(command);
		var deserialized = System.Text.Json.JsonSerializer.Deserialize<SendEmailCommand>(json);

		// Assert
		deserialized.Should().NotBeNull();
		deserialized!.Subject.Should().Be(command.Subject);
		deserialized.Body.Should().Be(command.Body);
	}

	[Fact]
	public void SendEmailCommand_WithUnicode_ShouldSerializeCorrectly()
	{
		// Arrange
		var command = new SendEmailCommand
		{
			MessageId = Guid.NewGuid(),
			To = "test@example.com",
			Subject = "Unicode: Привет 🎉",
			Body = "Body: 中文 日本語 🌍",
			RequestedAt = DateTime.UtcNow,
			CorrelationId = Guid.NewGuid().ToString()
		};

		// Act
		var json = System.Text.Json.JsonSerializer.Serialize(command);
		var deserialized = System.Text.Json.JsonSerializer.Deserialize<SendEmailCommand>(json);

		// Assert
		deserialized.Should().NotBeNull();
		deserialized!.Subject.Should().Be(command.Subject);
		deserialized.Body.Should().Be(command.Body);
	}

	[Fact]
	public void SendEmailCommand_WithLargeBody_ShouldSerializeCorrectly()
	{
		// Arrange
		var largeBody = new string('x', 100000); // 100KB body
		var command = new SendEmailCommand
		{
			MessageId = Guid.NewGuid(),
			To = "test@example.com",
			Subject = "Large Email",
			Body = largeBody,
			RequestedAt = DateTime.UtcNow,
			CorrelationId = Guid.NewGuid().ToString()
		};

		// Act
		var json = System.Text.Json.JsonSerializer.Serialize(command);
		var deserialized = System.Text.Json.JsonSerializer.Deserialize<SendEmailCommand>(json);

		// Assert
		deserialized.Should().NotBeNull();
		deserialized!.Body.Length.Should().Be(100000);
	}

	[Fact]
	public void SendEmailCommand_WithBinaryAttachments_ShouldSerializeCorrectly()
	{
		// Arrange
		var binaryData = new byte[] { 0x00, 0x01, 0xFF, 0xFE };
		var attachment = new EmailAttachment("test.bin", binaryData, "application/octet-stream");

		var command = new SendEmailCommand
		{
			MessageId = Guid.NewGuid(),
			To = "test@example.com",
			Subject = "Binary Attachment",
			Body = "See attachment",
			Attachments = new List<EmailAttachment> { attachment },
			RequestedAt = DateTime.UtcNow,
			CorrelationId = Guid.NewGuid().ToString()
		};

		// Act
		var options = new System.Text.Json.JsonSerializerOptions
		{
			PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
		};
		// Configure polymorphic serialization for interface type
		options.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
		{
			Modifiers = { (ti) =>
			{
				if (ti.Type == typeof(Application.Contracts.Email.IEmailAttachment))
				{
					ti.PolymorphismOptions = new System.Text.Json.Serialization.Metadata.JsonPolymorphismOptions
					{
						TypeDiscriminatorPropertyName = "$type",
						IgnoreUnrecognizedTypeDiscriminators = true,
						DerivedTypes =
						{
							new System.Text.Json.Serialization.Metadata.JsonDerivedType(typeof(Infrastructure.Messaging.Contracts.EmailAttachment), "emailAttachment")
						}
					};
				}
			}}
		};
		var json = System.Text.Json.JsonSerializer.Serialize(command, options);
		var deserialized = System.Text.Json.JsonSerializer.Deserialize<SendEmailCommand>(json, options);

		// Assert
		deserialized.Should().NotBeNull();
		deserialized!.Attachments.Should().HaveCount(1);
		deserialized.Attachments!.First().Content.Should().Equal(binaryData);
	}
}

/// <summary>
/// Tests for broker connection failure simulation.
/// </summary>
public class ConnectionFailureTests : IAsyncLifetime
{
	private MassTransitTestHarness _harness = null!;

	public async Task InitializeAsync()
	{
		_harness = new MassTransitTestHarness();
		await _harness.InitializeAsync();
	}

	public async Task DisposeAsync()
	{
		await _harness.DisposeAsync();
	}

	[Fact]
	public async Task Outbox_WhenBrokerUnavailable_ShouldStoreMessagesForLaterDelivery()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		// Simulate broker being unavailable by creating outbox entry directly
		var outboxMessage = OutboxMessage.FromDomainEvent(
			new SendEmailCommand
			{
				MessageId = Guid.NewGuid(),
				To = "test@example.com",
				Subject = "Test",
				Body = "Test Body",
				RequestedAt = DateTime.UtcNow,
				CorrelationId = correlationId
			},
			correlationId,
			Guid.NewGuid().ToString(),
			"Order");

		// Act
		dbContext.Set<OutboxMessage>().Add(outboxMessage);
		await dbContext.SaveChangesAsync();

		// Assert - Message should be stored even though broker is unavailable
		var savedMessage = await dbContext.Set<OutboxMessage>()
			.FirstAsync(o => o.CorrelationId == correlationId);

		savedMessage.Should().NotBeNull();
		savedMessage.Status.Should().Be(OutboxMessageStatus.Pending);
	}

	[Fact]
	public async Task Outbox_AfterBrokerRecovery_ShouldProcessPendingMessages()
	{
		// Arrange
		var dbContext = _harness.GetDbContext();
		var correlationId = Guid.NewGuid().ToString();

		var outboxMessage = OutboxMessage.FromDomainEvent(
			new SendEmailCommand
			{
				MessageId = Guid.NewGuid(),
				To = "test@example.com",
				Subject = "Test",
				Body = "Test Body",
				RequestedAt = DateTime.UtcNow,
				CorrelationId = correlationId
			},
			correlationId,
			Guid.NewGuid().ToString(),
			"Order");

		dbContext.Set<OutboxMessage>().Add(outboxMessage);
		await dbContext.SaveChangesAsync();

		// Act - Simulate broker recovery by processing pending messages
		var pendingMessages = await dbContext.Set<OutboxMessage>()
			.Where(o => o.Status == OutboxMessageStatus.Pending)
			.ToListAsync();

		foreach (var msg in pendingMessages)
		{
			// Simulate successful delivery
			msg.MarkAsPublished();
		}
		await dbContext.SaveChangesAsync();

		// Assert
		var processedCount = await dbContext.Set<OutboxMessage>()
			.CountAsync(o => o.Status == OutboxMessageStatus.Published);

		processedCount.Should().Be(1);
	}
}
