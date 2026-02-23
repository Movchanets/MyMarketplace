using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Messaging.Consumers;

namespace Application.Tests.Messaging
{
	public class MassTransitEmailServiceTests
	{
		[Fact]
		public async Task SendEmailAsync_PublishesMessageToBus()
		{
			// Arrange
			var mockBus = new Mock<IBus>();
			var mockLogger = new Mock<ILogger<MassTransitEmailService>>();
			var service = new MassTransitEmailService(mockBus.Object, mockLogger.Object);

			var command = new SendEmailCommand
			{
				MessageId = Guid.NewGuid(),
				To = "recipient@example.com",
				Subject = "Test",
				Body = "Body",
				IsHtml = false,
				RequestedAt = DateTime.UtcNow
			};

			// Act
			await service.SendEmailAsync(command, CancellationToken.None);

			// Assert
			mockBus.Verify(b => b.Publish(
				It.Is<SendEmailCommand>(m => m.MessageId == command.MessageId && m.To == "recipient@example.com"),
				It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task SendTemplatedEmailAsync_RendersAndPublishes()
		{
			// Arrange
			var mockBus = new Mock<IBus>();
			var mockLogger = new Mock<ILogger<MassTransitEmailService>>();
			var service = new MassTransitEmailService(mockBus.Object, mockLogger.Object);

			// Act
			await service.SendTemplatedEmailAsync("Welcome", "templated@example.com", new { Name = "User" }, CancellationToken.None);

			// Assert - message published with correct recipient and templated content
			mockBus.Verify(b => b.Publish(
				It.Is<SendEmailCommand>(m => m.To == "templated@example.com" && m.Subject == "Welcome!" && m.Body.Contains("Welcome to our store")),
				It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task ScheduleEmailAsync_PublishesAfterDelay_UsingHarness()
		{
			// Arrange - use MassTransit test harness to observe scheduled messages
			var harness = new MassTransitTestHarness(
				configureServices: services =>
				{
					services.AddScoped<Application.Contracts.Email.IEmailNotificationService, MassTransitEmailService>();
				},
				configureBus: configurator =>
				{
					configurator.UsingPostgres((context, cfg) =>
					{
						cfg.UseMessageRetry(r => r.Immediate(3));
						cfg.ConfigureEndpoints(context);
					});
				});
			await harness.InitializeAsync();
			try
			{
				var service = harness.Services.GetRequiredService<Application.Contracts.Email.IEmailNotificationService>();

				var command = new SendEmailCommand
				{
					MessageId = Guid.NewGuid(),
					To = "delayed@example.com",
					Subject = "Delayed",
					Body = "Body",
					IsHtml = false,
					RequestedAt = DateTime.UtcNow
				};

				var scheduledTime = DateTime.UtcNow.AddMinutes(1);

				// Act
				await service.ScheduleEmailAsync(command, scheduledTime);

				// Advance time to just before scheduled time - consumer should not have received the message yet
				harness.AdvanceTime(TimeSpan.FromSeconds(30));
				var consumerHarness = harness.GetConsumerHarness<SendEmailConsumer>();
				consumerHarness.Consumed.Select<SendEmailCommand>().Should().BeEmpty();

				// Advance past scheduled time and allow processing
				harness.AdvanceTime(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));
				await Task.Delay(500);

				// Assert - published message has Delay approximately equal to scheduledTime - now
				var published = harness.Harness.Published.Select<SendEmailCommand>().FirstOrDefault();
				published.Should().NotBeNull();
				var expectedDelay = scheduledTime - DateTime.UtcNow;
				var actualDelay = published!.Context.Delay ?? TimeSpan.Zero;
				actualDelay.Should().BeCloseTo(expectedDelay, TimeSpan.FromSeconds(2));
			}
			finally
			{
				await harness.DisposeAsync();
			}
		}
	}
}