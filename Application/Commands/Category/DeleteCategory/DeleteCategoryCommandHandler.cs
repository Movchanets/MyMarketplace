using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Category.DeleteCategory;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, ServiceResponse>
{
	private const string CategoriesAllCacheKey = "categories:all";
	private const string CategoriesTopLevelCacheKey = "categories:top-level";

	private readonly ICategoryRepository _categoryRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMemoryCache _cache;
	private readonly ILogger<DeleteCategoryCommandHandler> _logger;

	public DeleteCategoryCommandHandler(
		ICategoryRepository categoryRepository,
		IUnitOfWork unitOfWork,
		IMemoryCache cache,
		ILogger<DeleteCategoryCommandHandler> logger)
	{
		_categoryRepository = categoryRepository;
		_unitOfWork = unitOfWork;
		_cache = cache;
		_logger = logger;
	}

	public async Task<ServiceResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting category {CategoryId}", request.Id);

		try
		{
			var category = await _categoryRepository.GetByIdAsync(request.Id);
			if (category == null)
			{
				_logger.LogWarning("Category {CategoryId} not found", request.Id);
				return new ServiceResponse(false, "Category not found");
			}

			var subCategories = await _categoryRepository.GetSubCategoriesAsync(category.Id);
			if (subCategories.Any())
			{
				_logger.LogWarning("Category {CategoryId} has subcategories and cannot be deleted", request.Id);
				return new ServiceResponse(false, "Category has subcategories");
			}

			_categoryRepository.Delete(category);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_cache.Remove(CategoriesAllCacheKey);
			_cache.Remove(CategoriesTopLevelCacheKey);

			_logger.LogInformation("Category {CategoryId} deleted successfully", request.Id);
			return new ServiceResponse(true, "Category deleted successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting category {CategoryId}", request.Id);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
