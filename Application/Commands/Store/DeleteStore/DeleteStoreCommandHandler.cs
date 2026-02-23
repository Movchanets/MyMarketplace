using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Store.DeleteStore;

public sealed class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand, ServiceResponse>
{
	private readonly IStoreRepository _storeRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<DeleteStoreCommandHandler> _logger;

	public DeleteStoreCommandHandler(
		IStoreRepository storeRepository,
		IUnitOfWork unitOfWork,
		ILogger<DeleteStoreCommandHandler> logger)
	{
		_storeRepository = storeRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting store for user {UserId}", request.UserId);

		try
		{
			var store = await _storeRepository.GetByUserIdAsync(request.UserId);
			if (store == null)
			{
				_logger.LogWarning("Store for user {UserId} not found", request.UserId);
				return new ServiceResponse(false, "Store not found");
			}

			_storeRepository.Delete(store);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Store {StoreId} deleted successfully", store.Id);
			return new ServiceResponse(true, "Store deleted successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting store for user {UserId}", request.UserId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
