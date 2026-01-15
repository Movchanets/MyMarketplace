using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog;

public sealed record GetCategoriesQuery(Guid? ParentCategoryId = null, bool TopLevelOnly = false)
	: IRequest<ServiceResponse<IReadOnlyList<CategoryDto>>>;

public sealed class GetCategoriesQueryHandler
	: IRequestHandler<GetCategoriesQuery, ServiceResponse<IReadOnlyList<CategoryDto>>>
{
	private readonly ICategoryRepository _categoryRepository;
	private readonly ILogger<GetCategoriesQueryHandler> _logger;

	public GetCategoriesQueryHandler(
		ICategoryRepository categoryRepository,
		ILogger<GetCategoriesQueryHandler> logger)
	{
		_categoryRepository = categoryRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<IReadOnlyList<CategoryDto>>> Handle(
		GetCategoriesQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			IEnumerable<Domain.Entities.Category> categories;
			if (request.TopLevelOnly)
			{
				categories = await _categoryRepository.GetTopLevelAsync();
			}
			else if (request.ParentCategoryId.HasValue)
			{
				categories = await _categoryRepository.GetSubCategoriesAsync(request.ParentCategoryId.Value);
			}
			else
			{
				categories = await _categoryRepository.GetAllAsync();
			}

			var payload = categories
				.Select(MapCategory)
				.ToList()
				.AsReadOnly();

			return new ServiceResponse<IReadOnlyList<CategoryDto>>(true, "Categories retrieved successfully", payload);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving categories");
			return new ServiceResponse<IReadOnlyList<CategoryDto>>(false, $"Error: {ex.Message}");
		}
	}

	private static CategoryDto MapCategory(Domain.Entities.Category category)
		=> new(
			category.Id,
			category.Name,
			category.Slug,
			category.Description,
			category.Emoji,
			category.ParentCategoryId);
}

public sealed record GetCategoryByIdQuery(Guid Id)
	: IRequest<ServiceResponse<CategoryDto>>;

public sealed class GetCategoryByIdQueryHandler
	: IRequestHandler<GetCategoryByIdQuery, ServiceResponse<CategoryDto>>
{
	private readonly ICategoryRepository _categoryRepository;
	private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

	public GetCategoryByIdQueryHandler(
		ICategoryRepository categoryRepository,
		ILogger<GetCategoryByIdQueryHandler> logger)
	{
		_categoryRepository = categoryRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
	{
		try
		{
			var category = await _categoryRepository.GetByIdAsync(request.Id);
			if (category is null)
			{
				return new ServiceResponse<CategoryDto>(false, "Category not found");
			}

		return new ServiceResponse<CategoryDto>(true, "Category retrieved successfully",
				new CategoryDto(category.Id, category.Name, category.Slug, category.Description, category.Emoji, category.ParentCategoryId));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving category {CategoryId}", request.Id);
			return new ServiceResponse<CategoryDto>(false, $"Error: {ex.Message}");
		}
	}
}

public sealed record GetCategoryBySlugQuery(string Slug)
	: IRequest<ServiceResponse<CategoryDto>>;

public sealed class GetCategoryBySlugQueryHandler
	: IRequestHandler<GetCategoryBySlugQuery, ServiceResponse<CategoryDto>>
{
	private readonly ICategoryRepository _categoryRepository;
	private readonly ILogger<GetCategoryBySlugQueryHandler> _logger;

	public GetCategoryBySlugQueryHandler(
		ICategoryRepository categoryRepository,
		ILogger<GetCategoryBySlugQueryHandler> logger)
	{
		_categoryRepository = categoryRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<CategoryDto>> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
	{
		try
		{
			var slug = request.Slug?.Trim();
			if (string.IsNullOrWhiteSpace(slug))
			{
				return new ServiceResponse<CategoryDto>(false, "Slug is required");
			}

			var category = await _categoryRepository.GetBySlugAsync(slug);
			if (category is null)
			{
				return new ServiceResponse<CategoryDto>(false, "Category not found");
			}

			return new ServiceResponse<CategoryDto>(true, "Category retrieved successfully",
				new CategoryDto(category.Id, category.Name, category.Slug, category.Description, category.Emoji, category.ParentCategoryId));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving category by slug {Slug}", request.Slug);
			return new ServiceResponse<CategoryDto>(false, $"Error: {ex.Message}");
		}
	}
}
