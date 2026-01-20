using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Commands.Product.Sku.UpdateSku;

public sealed class UpdateSkuCommandHandler : IRequestHandler<UpdateSkuCommand, ServiceResponse>
{
	private readonly ISkuRepository _skuRepository;
	private readonly IAttributeDefinitionRepository _attributeDefinitionRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<UpdateSkuCommandHandler> _logger;

	public UpdateSkuCommandHandler(
		ISkuRepository skuRepository,
		IAttributeDefinitionRepository attributeDefinitionRepository,
		IUnitOfWork unitOfWork,
		ILogger<UpdateSkuCommandHandler> logger)
	{
		_skuRepository = skuRepository;
		_attributeDefinitionRepository = attributeDefinitionRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(UpdateSkuCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating SKU {SkuId} for product {ProductId} by user {UserId}", request.SkuId, request.ProductId, request.UserId);

		try
		{
			var sku = await _skuRepository.GetByIdAsync(request.SkuId);
			if (sku?.Product is null || sku.ProductId != request.ProductId || sku.Product.Store is null || sku.Product.Store.UserId != request.UserId)
			{
				_logger.LogWarning("SKU {SkuId} not found for product {ProductId} and user {UserId}", request.SkuId, request.ProductId, request.UserId);
				return new ServiceResponse(false, "SKU not found");
			}

			if (sku.Product.Store.IsSuspended)
			{
				_logger.LogWarning("Store {StoreId} is suspended", sku.Product.Store.Id);
				return new ServiceResponse(false, "Store is suspended");
			}

		sku.UpdatePrice(request.Price);
		sku.UpdateStock(request.StockQuantity);

		// Update attributes if provided
		if (request.Attributes is not null)
		{
			// Clear existing typed attributes
			sku.ClearTypedAttributes();
			
			// Update JSONB attributes
			foreach (var attr in request.Attributes)
			{
				sku.SetAttribute(attr.Key, attr.Value);
			}
			
			// Convert to typed attributes
			var attributeDefinitions = await _attributeDefinitionRepository.GetAllAsync();
			sku.SetTypedAttributes(request.Attributes, attributeDefinitions);
		}

		_skuRepository.Update(sku);

		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return new ServiceResponse(true, "SKU updated successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating SKU {SkuId} for product {ProductId}", request.SkuId, request.ProductId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
