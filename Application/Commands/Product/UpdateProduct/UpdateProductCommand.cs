using Application.Behaviors;
using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Commands.Product.UpdateProduct;

public sealed record UpdateProductCommand(
	Guid UserId,
	Guid ProductId,
	string Name,
	string? Description,
	List<Guid> CategoryIds,
	List<Guid>? TagIds = null,
	Guid? PrimaryCategoryId = null,
	List<GalleryImageUploadRequest>? NewGalleryImages = null,
	List<Guid>? GalleryIdsToDelete = null
) : IRequest<ServiceResponse>, ICacheInvalidatingCommand
{
	public IEnumerable<string> CacheTags => ["products"];

	public UpdateProductCommand(
		Guid UserId,
		Guid ProductId,
		string Name,
		string? Description,
		Guid CategoryId,
		List<Guid>? TagIds = null)
		: this(UserId, ProductId, Name, Description, new List<Guid> { CategoryId }, TagIds)
	{
	}
}
