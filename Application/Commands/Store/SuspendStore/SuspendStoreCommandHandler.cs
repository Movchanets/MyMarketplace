using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Store.SuspendStore;

public sealed class SuspendStoreCommandHandler : IRequestHandler<SuspendStoreCommand, ServiceResponse>
{
	private readonly IStoreRepository _storeRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<SuspendStoreCommandHandler> _logger;

	public SuspendStoreCommandHandler(
		IStoreRepository storeRepository,
		IUnitOfWork unitOfWork,
		ILogger<SuspendStoreCommandHandler> logger)
	{
		_storeRepository = storeRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(SuspendStoreCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Suspending store {StoreId}", request.StoreId);

		try
		{
			var store = await _storeRepository.GetByIdAsync(request.StoreId);
			if (store == null)
			{
				_logger.LogWarning("Store {StoreId} not found", request.StoreId);
				return new ServiceResponse(false, "Store not found");
			}

			store.Suspend();
			_storeRepository.Update(store);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Store {StoreId} suspended successfully", store.Id);
			return new ServiceResponse(true, "Store suspended successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error suspending store {StoreId}", request.StoreId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
