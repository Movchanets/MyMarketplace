using Application.DTOs;
using Application.Interfaces;
using Domain.Helpers;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Store.UpdateStore;

public sealed class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, ServiceResponse>
{
	private readonly IStoreRepository _storeRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<UpdateStoreCommandHandler> _logger;

	public UpdateStoreCommandHandler(
		IStoreRepository storeRepository,
		IUserRepository userRepository,
		IUnitOfWork unitOfWork,
		ILogger<UpdateStoreCommandHandler> logger)
	{
		_storeRepository = storeRepository;
		_userRepository = userRepository;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating store for user {UserId}", request.UserId);

		try
		{
			// UserId from JWT is IdentityUserId, need to lookup DomainUser
			var domainUser = await _userRepository.GetByIdentityUserIdAsync(request.UserId);
			if (domainUser == null)
			{
				_logger.LogWarning("Domain user not found for identity user {UserId}", request.UserId);
				return new ServiceResponse(false, "User not found");
			}

			var store = await _storeRepository.GetByUserIdAsync(domainUser.Id);
			if (store == null)
			{
				_logger.LogWarning("Store for user {UserId} not found", domainUser.Id);
				return new ServiceResponse(false, "Store not found");
			}

			var newSlug = SlugHelper.GenerateSlug(request.Name);
			var existingBySlug = await _storeRepository.GetBySlugAsync(newSlug);
			if (existingBySlug != null && existingBySlug.Id != store.Id)
			{
				_logger.LogWarning("Store slug already exists {Slug}", newSlug);
				return new ServiceResponse(false, "Store with same slug already exists");
			}

			store.UpdateDetails(request.Name, request.Description);
			_storeRepository.Update(store);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Store {StoreId} updated successfully", store.Id);
			return new ServiceResponse(true, "Store updated successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating store for user {UserId}", request.UserId);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
