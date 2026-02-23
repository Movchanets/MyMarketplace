using Application.DTOs;
using Application.Interfaces;
using CategoryEntity = Domain.Entities.Category;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Category.CreateCategory;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, ServiceResponse<Guid>>
{
	private const string CategoriesAllCacheKey = "categories:all";
	private const string CategoriesTopLevelCacheKey = "categories:top-level";

	private readonly ICategoryRepository _categoryRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMemoryCache _cache;
	private readonly ILogger<CreateCategoryCommandHandler> _logger;

	public CreateCategoryCommandHandler(
		ICategoryRepository categoryRepository,
		IUnitOfWork unitOfWork,
		IMemoryCache cache,
		ILogger<CreateCategoryCommandHandler> logger)
	{
		_categoryRepository = categoryRepository;
		_unitOfWork = unitOfWork;
		_cache = cache;
		_logger = logger;
	}

	public async Task<ServiceResponse<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Creating category {CategoryName}", request.Name);

		try
		{
		var category = CategoryEntity.Create(request.Name, request.Description, request.ParentCategoryId);
			
			// Set emoji if provided
			if (!string.IsNullOrWhiteSpace(request.Emoji))
			{
				category.SetEmoji(request.Emoji);
			}

			// Parent validation + set navigation
			if (request.ParentCategoryId.HasValue)
			{
				var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value);
				if (parent == null)
				{
					_logger.LogWarning("Parent category {ParentCategoryId} not found", request.ParentCategoryId);
					return new ServiceResponse<Guid>(false, "Parent category not found");
				}

				category.SetParent(parent);
			}

			// Slug must be unique (enforced by DB index)
			var existingBySlug = await _categoryRepository.GetBySlugAsync(category.Slug);
			if (existingBySlug != null)
			{
				_logger.LogWarning("Category slug already exists {Slug}", category.Slug);
				return new ServiceResponse<Guid>(false, "Category with same slug already exists");
			}

			_categoryRepository.Add(category);
			await _unitOfWork.SaveChangesAsync(cancellationToken);

			// Invalidate categories cache (used by future category queries)
			_cache.Remove(CategoriesAllCacheKey);
			_cache.Remove(CategoriesTopLevelCacheKey);

			_logger.LogInformation("Category {CategoryId} created successfully", category.Id);
			return new ServiceResponse<Guid>(true, "Category created successfully", category.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating category {CategoryName}", request.Name);
			return new ServiceResponse<Guid>(false, $"Error: {ex.Message}");
		}
	}
}
