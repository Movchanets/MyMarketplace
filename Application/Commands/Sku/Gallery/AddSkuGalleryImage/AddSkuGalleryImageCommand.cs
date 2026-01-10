using Application.DTOs;
using MediatR;

namespace Application.Commands.Sku.Gallery.AddSkuGalleryImage;

public sealed record AddSkuGalleryImageCommand(
	Guid UserId,
	Guid ProductId,
	Guid SkuId,
	Guid MediaImageId,
	int DisplayOrder = 0
) : IRequest<ServiceResponse<Guid>>;
