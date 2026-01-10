using System.Security.Claims;
using Application.Commands.Product.CreateProduct;
using Application.Commands.Product.DeleteProduct;
using Application.Commands.Product.Gallery.AddProductGalleryImage;
using Application.Commands.Product.Gallery.RemoveProductGalleryImage;
using Application.Commands.Product.SetProductBaseImage;
using Application.Commands.Product.Sku.AddSkuToProduct;
using Application.Commands.Product.Sku.DeleteSku;
using Application.Commands.Product.Sku.UpdateSku;
using Application.Commands.Product.ToggleProductActive;
using Application.Commands.Product.UpdateProduct;
using Application.Commands.Sku.Gallery.AddSkuGalleryImage;
using Application.Commands.Sku.Gallery.RemoveSkuGalleryImage;
using Application.Interfaces;
using Application.Queries.Catalog.GetMyProducts;
using Application.Queries.Catalog.GetProductById;
using Application.Queries.Catalog.GetProductBySkuCode;
using Application.Queries.Catalog.GetProductBySlug;
using Application.Queries.Catalog.GetProducts;
using Application.Queries.Catalog.GetProductsByCategoryId;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace API.Controllers;

public sealed record SetBaseImageRequest(string? BaseImageUrl);

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly IFileStorage _fileStorage;
	private readonly IMediaImageRepository _mediaImageRepository;

	public ProductsController(
		IMediator mediator,
		IFileStorage fileStorage,
		IMediaImageRepository mediaImageRepository)
	{
		_mediator = mediator;
		_fileStorage = fileStorage;
		_mediaImageRepository = mediaImageRepository;
	}

	/// <summary>
	/// Отримати список продуктів
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	[OutputCache(PolicyName = "Products")]
	public async Task<IActionResult> GetAll()
	{
		var result = await _mediator.Send(new GetProductsQuery());
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати продукт по Id
	/// </summary>
	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "ProductDetails")]
	public async Task<IActionResult> GetById([FromRoute] Guid id)
	{
		var result = await _mediator.Send(new GetProductByIdQuery(id));
		if (!result.IsSuccess) return NotFound(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати продукт по slug (з опціональним sku code)
	/// </summary>
	[HttpGet("s/{productSlug}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "ProductDetails")]
	public async Task<IActionResult> GetBySlug([FromRoute] string productSlug, [FromQuery] string? sku = null)
	{
		var result = await _mediator.Send(new GetProductBySlugQuery(productSlug, sku));
		if (!result.IsSuccess) return NotFound(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати продукт по skuCode
	/// </summary>
	[HttpGet("by-sku/{skuCode}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "ProductDetails")]
	public async Task<IActionResult> GetBySkuCode([FromRoute] string skuCode)
	{
		var result = await _mediator.Send(new GetProductBySkuCodeQuery(skuCode));
		if (!result.IsSuccess) return NotFound(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати продукти по categoryId
	/// </summary>
	[HttpGet("by-category/{categoryId:guid}")]
	[AllowAnonymous]
	[OutputCache(PolicyName = "ProductsByCategory")]
	public async Task<IActionResult> GetByCategory([FromRoute] Guid categoryId)
	{
		var result = await _mediator.Send(new GetProductsByCategoryIdQuery(categoryId));
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Отримати продукти поточного продавця
	/// </summary>
	[HttpGet("my")]
	[Authorize(Policy = "Permission:products.read.self")]
	public async Task<IActionResult> GetMyProducts()
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var result = await _mediator.Send(new GetMyProductsQuery(userId));
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	public sealed record CreateProductRequest(
		string Name,
		string? Description,
		List<Guid>? CategoryIds,
		List<Guid>? TagIds = null,
		List<SkuRequest>? Skus = null
	);

	public sealed record SkuRequest(
		decimal Price,
		int StockQuantity,
		Dictionary<string, object?>? Attributes = null
	);

	/// <summary>
	/// Створити продукт (для поточного продавця)
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "Permission:products.create")]
	public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		// If no SKUs provided, create with default values (inactive product)
		var firstSku = request.Skus?.FirstOrDefault();
		var command = new CreateProductCommand(
			userId,
			request.Name,
			request.Description,
			request.CategoryIds ?? new List<Guid>(),
			firstSku?.Price ?? 0,
			firstSku?.StockQuantity ?? 0,
			firstSku?.Attributes,
			request.TagIds
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);

		// Add additional SKUs if provided
		if (request.Skus is { Count: > 1 })
		{
			for (int i = 1; i < request.Skus.Count; i++)
			{
				var sku = request.Skus[i];
				var addSkuCmd = new AddSkuToProductCommand(
					userId,
					result.Payload,
					sku.Price,
					sku.StockQuantity,
					sku.Attributes
				);
				await _mediator.Send(addSkuCmd);
			}
		}

		return Ok(result);
	}

	public sealed record UpdateProductRequest(
		string Name,
		string? Description,
		List<Guid>? CategoryIds,
		List<Guid>? TagIds = null,
		Guid? PrimaryCategoryId = null,
		List<Guid>? GalleryIdsToDelete = null
	);

	/// <summary>
	/// Оновити продукт (JSON, без завантаження файлів)
	/// </summary>
	[HttpPut("{productId:guid}")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> Update(
		[FromRoute] Guid productId,
		[FromBody] UpdateProductRequest request)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new UpdateProductCommand(
			userId,
			productId,
			request.Name,
			request.Description,
			request.CategoryIds ?? new List<Guid>(),
			request.TagIds,
			request.PrimaryCategoryId,
			null, // No gallery images in JSON endpoint
			request.GalleryIdsToDelete
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	public sealed record UpdateProductWithGalleryRequest(
		string Name,
		string? Description,
		List<Guid>? CategoryIds,
		List<Guid>? TagIds = null,
		Guid? PrimaryCategoryId = null,
		List<Guid>? GalleryIdsToDelete = null
	);

	/// <summary>
	/// Оновити продукт з галереєю (multipart/form-data)
	/// </summary>
	[HttpPut("{productId:guid}/with-gallery")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> UpdateWithGallery(
		[FromRoute] Guid productId,
		[FromForm] UpdateProductWithGalleryRequest request,
		[FromForm] List<IFormFile>? newGalleryImages = null)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		// Prepare gallery images for command
		List<GalleryImageUploadRequest>? galleryUploads = null;
		if (newGalleryImages is { Count: > 0 })
		{
			galleryUploads = new List<GalleryImageUploadRequest>();
			for (int i = 0; i < newGalleryImages.Count; i++)
			{
				var file = newGalleryImages[i];
				galleryUploads.Add(new GalleryImageUploadRequest(
					file.OpenReadStream(),
					file.FileName,
					file.ContentType,
					i // DisplayOrder
				));
			}
		}

		var command = new UpdateProductCommand(
			userId,
			productId,
			request.Name,
			request.Description,
			request.CategoryIds ?? new List<Guid>(),
			request.TagIds,
			request.PrimaryCategoryId,
			galleryUploads,
			request.GalleryIdsToDelete
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	public sealed record ToggleActiveRequest(bool IsActive);

	/// <summary>
	/// Активувати/деактивувати продукт
	/// </summary>
	[HttpPatch("{productId:guid}/active")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> ToggleActive([FromRoute] Guid productId, [FromBody] ToggleActiveRequest request)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new ToggleProductActiveCommand(userId, productId, request.IsActive);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	public sealed record AddSkuRequest(
		decimal Price,
		int StockQuantity,
		Dictionary<string, object?>? Attributes = null
	);

	/// <summary>
	/// Додати SKU до продукту
	/// </summary>
	[HttpPost("{productId:guid}/skus")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> AddSku([FromRoute] Guid productId, [FromBody] AddSkuRequest request)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new AddSkuToProductCommand(
			userId,
			productId,
			request.Price,
			request.StockQuantity,
			request.Attributes
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	public sealed record UpdateSkuRequest(
		decimal Price,
		int StockQuantity,
		Dictionary<string, object?>? Attributes = null
	);

	/// <summary>
	/// Оновити SKU продукту
	/// </summary>
	[HttpPut("{productId:guid}/skus/{skuId:guid}")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> UpdateSku([FromRoute] Guid productId, [FromRoute] Guid skuId, [FromBody] UpdateSkuRequest request)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new UpdateSkuCommand(
			userId,
			productId,
			skuId,
			request.Price,
			request.StockQuantity,
			request.Attributes
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Видалити SKU з продукту
	/// </summary>
	[HttpDelete("{productId:guid}/skus/{skuId:guid}")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> DeleteSku([FromRoute] Guid productId, [FromRoute] Guid skuId)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new DeleteSkuCommand(userId, productId, skuId);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Завантажити зображення до галереї продукту
	/// </summary>
	[HttpPost("{productId:guid}/gallery")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> UploadGalleryImage([FromRoute] Guid productId, IFormFile file, [FromForm] int displayOrder = 0)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		if (file == null || file.Length == 0)
		{
			return BadRequest(new { IsSuccess = false, Message = "File is required" });
		}

		// Upload file to storage
		using var stream = file.OpenReadStream();
		var storageKey = await _fileStorage.UploadAsync(
			stream,
			$"products/{productId}/{file.FileName}",
			file.ContentType
		);

		var publicUrl = _fileStorage.GetPublicUrl(storageKey);

		// Create MediaImage
		var mediaImage = new MediaImage(
			storageKey,
			file.ContentType,
			0, 0, // Width/Height - could be extracted from image
			file.FileName
		);
		await _mediaImageRepository.AddAsync(mediaImage);

		// Add to product gallery
		var command = new AddProductGalleryImageCommand(
			userId,
			productId,
			mediaImage.Id,
			displayOrder
		);

		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);

		return Ok(new
		{
			IsSuccess = true,
			Message = "Image uploaded successfully",
			Payload = new
			{
				GalleryId = result.Payload,
				MediaImageId = mediaImage.Id,
				Url = publicUrl,
				StorageKey = storageKey
			}
		});
	}

	/// <summary>
	/// Видалити зображення з галереї продукту
	/// </summary>
	[HttpDelete("{productId:guid}/gallery/{galleryId:guid}")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> DeleteGalleryImage([FromRoute] Guid productId, [FromRoute] Guid galleryId)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new RemoveProductGalleryImageCommand(userId, productId, galleryId);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	/// <summary>
	/// Встановити базове зображення продукту
	/// </summary>
	[HttpPatch("{productId:guid}/base-image")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> SetBaseImage([FromRoute] Guid productId, [FromBody] SetBaseImageRequest request)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new SetProductBaseImageCommand(userId, productId, request.BaseImageUrl);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	#region SKU Gallery

	/// <summary>
	/// Завантажити зображення до галереї SKU варіанту
	/// </summary>
	[HttpPost("{productId:guid}/skus/{skuId:guid}/gallery")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> UploadSkuGalleryImage(
		[FromRoute] Guid productId,
		[FromRoute] Guid skuId,
		IFormFile file,
		[FromForm] int displayOrder = 0)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		if (file == null || file.Length == 0)
		{
			return BadRequest(new { IsSuccess = false, Message = "File is required" });
		}

		// Upload file to storage
		using var stream = file.OpenReadStream();
		var storageKey = await _fileStorage.UploadAsync(
			stream,
			$"products/{productId}/skus/{skuId}/{file.FileName}",
			file.ContentType
		);

		var publicUrl = _fileStorage.GetPublicUrl(storageKey);

		// Create MediaImage
		var mediaImage = new MediaImage(
			storageKey,
			file.ContentType,
			0, 0, // Will be updated if image processing is added
			Path.GetFileNameWithoutExtension(file.FileName)
		);
		await _mediaImageRepository.AddAsync(mediaImage);

		// Add to SKU gallery
		var command = new AddSkuGalleryImageCommand(userId, productId, skuId, mediaImage.Id, displayOrder);
		var result = await _mediator.Send(command);

		if (!result.IsSuccess) return BadRequest(result);

		return Ok(new
		{
			result.IsSuccess,
			result.Message,
			GalleryId = result.Payload,
			MediaImageId = mediaImage.Id,
			Url = publicUrl
		});
	}

	/// <summary>
	/// Видалити зображення з галереї SKU
	/// </summary>
	[HttpDelete("{productId:guid}/skus/{skuId:guid}/gallery/{galleryItemId:guid}")]
	[Authorize(Policy = "Permission:products.update.self")]
	public async Task<IActionResult> DeleteSkuGalleryImage(
		[FromRoute] Guid productId,
		[FromRoute] Guid skuId,
		[FromRoute] Guid galleryItemId)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new RemoveSkuGalleryImageCommand(userId, productId, skuId, galleryItemId);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}

	#endregion

	/// <summary>
	/// Видалити продукт
	/// </summary>
	[HttpDelete("{productId:guid}")]
	[Authorize(Policy = "Permission:products.delete.self")]
	public async Task<IActionResult> Delete([FromRoute] Guid productId)
	{
		var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();
		if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();

		var command = new DeleteProductCommand(userId, productId);
		var result = await _mediator.Send(command);
		if (!result.IsSuccess) return BadRequest(result);
		return Ok(result);
	}
}
