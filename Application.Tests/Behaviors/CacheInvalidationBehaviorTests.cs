using Application.Behaviors;
using Application.DTOs;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Application.Tests.Behaviors;

public class CacheInvalidationBehaviorTests
{
	private readonly Mock<ICacheInvalidationService> _cacheInvalidationMock;
	private readonly Mock<ILogger<CacheInvalidationBehavior<TestCommand, ServiceResponse>>> _loggerMock;
	private readonly CacheInvalidationBehavior<TestCommand, ServiceResponse> _behavior;

	public CacheInvalidationBehaviorTests()
	{
		_cacheInvalidationMock = new Mock<ICacheInvalidationService>();
		_loggerMock = new Mock<ILogger<CacheInvalidationBehavior<TestCommand, ServiceResponse>>>();
		_behavior = new CacheInvalidationBehavior<TestCommand, ServiceResponse>(
			_loggerMock.Object,
			_cacheInvalidationMock.Object);
	}

	[Fact]
	public async Task Handle_WhenCommandImplementsICacheInvalidating_AndResponseIsSuccess_ShouldInvalidateCache()
	{
		// Arrange
		var command = new TestCommand("test");
		var successResponse = new ServiceResponse(true, "Success");

		// Act
		var result = await _behavior.Handle(
			command,
			ct => Task.FromResult(successResponse),
			CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		_cacheInvalidationMock.Verify(
			x => x.InvalidateByTagsAsync(
				It.Is<IEnumerable<string>>(tags => tags.Contains("test-tag")),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task Handle_WhenCommandImplementsICacheInvalidating_AndResponseIsFailure_ShouldNotInvalidateCache()
	{
		// Arrange
		var command = new TestCommand("test");
		var failureResponse = new ServiceResponse(false, "Failed");

		// Act
		var result = await _behavior.Handle(
			command,
			ct => Task.FromResult(failureResponse),
			CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		_cacheInvalidationMock.Verify(
			x => x.InvalidateByTagsAsync(
				It.IsAny<IEnumerable<string>>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task Handle_WhenCacheServiceIsNull_ShouldNotThrow()
	{
		// Arrange
		var behaviorWithNullService = new CacheInvalidationBehavior<TestCommand, ServiceResponse>(
			_loggerMock.Object,
			null);
		var command = new TestCommand("test");
		var successResponse = new ServiceResponse(true, "Success");

		// Act
		var act = async () => await behaviorWithNullService.Handle(
			command,
			ct => Task.FromResult(successResponse),
			CancellationToken.None);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task Handle_WhenCommandHasNoTags_ShouldNotCallInvalidation()
	{
		// Arrange
		var emptyTagsBehavior = new CacheInvalidationBehavior<TestCommandWithEmptyTags, ServiceResponse>(
			new Mock<ILogger<CacheInvalidationBehavior<TestCommandWithEmptyTags, ServiceResponse>>>().Object,
			_cacheInvalidationMock.Object);
		var command = new TestCommandWithEmptyTags();
		var successResponse = new ServiceResponse(true, "Success");

		// Act
		await emptyTagsBehavior.Handle(
			command,
			ct => Task.FromResult(successResponse),
			CancellationToken.None);

		// Assert
		_cacheInvalidationMock.Verify(
			x => x.InvalidateByTagsAsync(
				It.IsAny<IEnumerable<string>>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task Handle_WhenCommandDoesNotImplementICacheInvalidating_ShouldNotCallInvalidation()
	{
		// Arrange
		var nonCacheBehavior = new CacheInvalidationBehavior<NonCacheCommand, ServiceResponse>(
			new Mock<ILogger<CacheInvalidationBehavior<NonCacheCommand, ServiceResponse>>>().Object,
			_cacheInvalidationMock.Object);
		var command = new NonCacheCommand();
		var successResponse = new ServiceResponse(true, "Success");

		// Act
		await nonCacheBehavior.Handle(
			command,
			ct => Task.FromResult(successResponse),
			CancellationToken.None);

		// Assert
		_cacheInvalidationMock.Verify(
			x => x.InvalidateByTagsAsync(
				It.IsAny<IEnumerable<string>>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	// Test command that implements ICacheInvalidatingCommand
	public record TestCommand(string Name) : IRequest<ServiceResponse>, ICacheInvalidatingCommand
	{
		public IEnumerable<string> CacheTags => ["test-tag", "another-tag"];
	}

	// Test command with empty tags
	public record TestCommandWithEmptyTags : IRequest<ServiceResponse>, ICacheInvalidatingCommand
	{
		public IEnumerable<string> CacheTags => [];
	}

	// Test command that doesn't implement ICacheInvalidatingCommand
	public record NonCacheCommand : IRequest<ServiceResponse>;
}
