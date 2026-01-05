using Application.DTOs;
using Application.Interfaces;
using CategoryEntity = Domain.Entities.Category;
using Domain.Helpers;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Category.UpdateCategory;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, ServiceResponse>
{
	private const string CategoriesAllCacheKey = "categories:all";
	private const string CategoriesTopLevelCacheKey = "categories:top-level";

	private readonly ICategoryRepository _categoryRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMemoryCache _cache;
	private readonly ILogger<UpdateCategoryCommandHandler> _logger;

	public UpdateCategoryCommandHandler(
		ICategoryRepository categoryRepository,
		IUnitOfWork unitOfWork,
		IMemoryCache cache,
		ILogger<UpdateCategoryCommandHandler> logger)
	{
		_categoryRepository = categoryRepository;
		_unitOfWork = unitOfWork;
		_cache = cache;
		_logger = logger;
	}
	
	public async Task<ServiceResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Updating category {CategoryId}", request.Id);

		try
		{
			var category = await _categoryRepository.GetByIdAsync(request.Id);
			if (category == null)
			{
				_logger.LogWarning("Category {CategoryId} not found", request.Id);
				return new ServiceResponse(false, "Category not found");
			}

			// Slug uniqueness check before applying rename
			var newSlug = SlugHelper.GenerateSlug(request.Name);
			var existingBySlug = await _categoryRepository.GetBySlugAsync(newSlug);
			if (existingBySlug != null && existingBySlug.Id != category.Id)
			{
				_logger.LogWarning("Category slug already exists {Slug}", newSlug);
				return new ServiceResponse(false, "Category with same slug already exists");
			}

			category.Rename(request.Name);
			category.UpdateDescription(request.Description);

			if (request.ParentCategoryId.HasValue)
			{
				var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value);
				if (parent == null)
				{
					_logger.LogWarning("Parent category {ParentCategoryId} not found", request.ParentCategoryId);
					return new ServiceResponse(false, "Parent category not found");
				}

				category.SetParent(parent);
			}
			else
			{
				category.SetParent(null);
			}

			_categoryRepository.Update(category);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			_cache.Remove(CategoriesAllCacheKey);
			_cache.Remove(CategoriesTopLevelCacheKey);

			_logger.LogInformation("Category {CategoryId} updated successfully", request.Id);
			return new ServiceResponse(true, "Category updated successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating category {CategoryId}", request.Id);
			return new ServiceResponse(false, $"Error: {ex.Message}");
		}
	}
}
