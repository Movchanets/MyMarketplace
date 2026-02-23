using Application.Commands.Tag.CreateTag;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

using DomainTag = Domain.Entities.Tag;

namespace Application.Tests.Commands.Tag;

public class CreateTagCommandHandlerTests
{
	[Fact]
	public async Task Handle_WhenSlugNotExists_CreatesTag()
	{
		// Arrange
		var tagRepository = new Mock<ITagRepository>(MockBehavior.Strict);
		var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
		var logger = new Mock<ILogger<CreateTagCommandHandler>>();

		tagRepository
			.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
			.ReturnsAsync((DomainTag?)null);

		tagRepository
			.Setup(x => x.Add(It.IsAny<DomainTag>()))
			.Verifiable();

		unitOfWork
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(1);

		var handler = new CreateTagCommandHandler(tagRepository.Object, unitOfWork.Object, logger.Object);

		var command = new CreateTagCommand("New Tag", "desc");

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeEmpty();
		tagRepository.Verify(x => x.GetBySlugAsync(It.IsAny<string>()), Times.Once);
		tagRepository.Verify(x => x.Add(It.IsAny<DomainTag>()), Times.Once);
		unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Handle_WhenSlugExists_ReturnsFailure()
	{
		// Arrange
		var tagRepository = new Mock<ITagRepository>(MockBehavior.Strict);
		var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
		var logger = new Mock<ILogger<CreateTagCommandHandler>>();

		tagRepository
			.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
			.ReturnsAsync(DomainTag.Create("Existing"));

		var handler = new CreateTagCommandHandler(tagRepository.Object, unitOfWork.Object, logger.Object);
		var command = new CreateTagCommand("Existing");

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Be("Tag with same slug already exists");
		tagRepository.Verify(x => x.GetBySlugAsync(It.IsAny<string>()), Times.Once);
		tagRepository.Verify(x => x.Add(It.IsAny<DomainTag>()), Times.Never);
		unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Handle_WhenExceptionThrown_ReturnsFailure()
	{
		// Arrange
		var tagRepository = new Mock<ITagRepository>(MockBehavior.Strict);
		var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
		var logger = new Mock<ILogger<CreateTagCommandHandler>>();

		tagRepository
			.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
			.ThrowsAsync(new InvalidOperationException("boom"));

		var handler = new CreateTagCommandHandler(tagRepository.Object, unitOfWork.Object, logger.Object);
		var command = new CreateTagCommand("Any");

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.Message.Should().Contain("boom");
		tagRepository.Verify(x => x.GetBySlugAsync(It.IsAny<string>()), Times.Once);
		unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}
}
