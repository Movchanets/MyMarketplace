using Application.DTOs;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Queries.Catalog.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, ServiceResponse<CategoryDto>>
{
	private readonly ICategoryRepository _categoryRepository;
	private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

	public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository, ILogger<GetCategoryByIdQueryHandler> logger)
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
