using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Store.VerifyStore;

public sealed class VerifyStoreCommandHandler : IRequestHandler<VerifyStoreCommand, ServiceResponse>
{
	private readonly IStoreRepository _storeRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<VerifyStoreCommandHandler> _logger;

	public VerifyStoreCommandHandler(
		IStoreRepository storeRepository,
		IUnitOfWork unitOfWork,
		ILogger<VerifyStoreCommandHandler> logger)
	{
		_storeRepository = storeRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(VerifyStoreCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Verifying store {StoreId}", request.StoreId);

		try
		{
			var store = await _storeRepository.GetByIdAsync(request.StoreId);
			if (store == null)
			{
				_logger.LogWarning("Store {StoreId} not found", request.StoreId);
				return new ServiceResponse(false, "Store not found");
			}

			store.Verify();
			_storeRepository.Update(store);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Store {StoreId} verified successfully", store.Id);
			return new ServiceResponse(true, "Store verified successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error verifying store {StoreId}", request.StoreId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
