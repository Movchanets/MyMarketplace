using Application.DTOs;
using MediatR;

namespace Application.Commands.Sku.Gallery.RemoveSkuGalleryImage;

public sealed record RemoveSkuGalleryImageCommand(
	Guid UserId,
	Guid ProductId,
	Guid SkuId,
	Guid GalleryItemId
) : IRequest<ServiceResponse>;
