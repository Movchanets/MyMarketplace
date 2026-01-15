using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog.GetCategoryBySlug;

public sealed class GetCategoryBySlugQueryHandler : IRequestHandler<GetCategoryBySlugQuery, ServiceResponse<CategoryDto>>
{
	private readonly ICategoryRepository _categoryRepository;
	private readonly ILogger<GetCategoryBySlugQueryHandler> _logger;

	public GetCategoryBySlugQueryHandler(ICategoryRepository categoryRepository, ILogger<GetCategoryBySlugQueryHandler> logger)
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
