using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Store.SuspendStore;

public sealed class UnsuspendStoreCommandHandler : IRequestHandler<UnsuspendStoreCommand, ServiceResponse>
{
	private readonly IStoreRepository _storeRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<UnsuspendStoreCommandHandler> _logger;

	public UnsuspendStoreCommandHandler(
		IStoreRepository storeRepository,
		IUnitOfWork unitOfWork,
		ILogger<UnsuspendStoreCommandHandler> logger)
	{
		_storeRepository = storeRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(UnsuspendStoreCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Unsuspending store {StoreId}", request.StoreId);

		try
		{
			var store = await _storeRepository.GetByIdAsync(request.StoreId);
			if (store == null)
			{
				_logger.LogWarning("Store {StoreId} not found", request.StoreId);
				return new ServiceResponse(false, "Store not found");
			}

			store.Unsuspend();
			_storeRepository.Update(store);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Store {StoreId} unsuspended successfully", store.Id);
			return new ServiceResponse(true, "Store unsuspended successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error unsuspending store {StoreId}", request.StoreId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
