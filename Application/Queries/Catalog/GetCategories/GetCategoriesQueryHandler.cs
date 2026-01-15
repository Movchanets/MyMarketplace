using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog.GetCategories;

public sealed class GetCategoriesQueryHandler
	: IRequestHandler<GetCategoriesQuery, ServiceResponse<IReadOnlyList<CategoryDto>>>
{
	private readonly ICategoryRepository _categoryRepository;
	private readonly ILogger<GetCategoriesQueryHandler> _logger;

	public GetCategoriesQueryHandler(ICategoryRepository categoryRepository, ILogger<GetCategoriesQueryHandler> logger)
	{
		_categoryRepository = categoryRepository;
		_logger = logger;
	}

	public async Task<ServiceResponse<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
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
		=> new(category.Id, category.Name, category.Slug, category.Description, category.Emoji, category.ParentCategoryId);
}
