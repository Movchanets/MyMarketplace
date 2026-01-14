using Application.DTOs;
using Application.Interfaces;
using Application.Queries.Catalog;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace API.Controllers;

/// <summary>
/// API для пошуку товарів та популярних запитів
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SearchController : ControllerBase
{
	private readonly IProductRepository _productRepository;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly ILogger<SearchController> _logger;

	public SearchController(
		IProductRepository productRepository,
		IServiceScopeFactory serviceScopeFactory,
		ILogger<SearchController> logger)
	{
		_productRepository = productRepository;
		_serviceScopeFactory = serviceScopeFactory;
		_logger = logger;
	}

	/// <summary>
	/// Пошук товарів по тексту
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 20)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(q))
			{
				return Ok(new ServiceResponse<IReadOnlyList<ProductSummaryDto>>(true, "Empty query", Array.Empty<ProductSummaryDto>()));
			}

			// Limit max results
			limit = Math.Clamp(limit, 1, 50);

			var products = await _productRepository.SearchAsync(q, limit);
			var results = products.Select(ProductMapping.MapSummary).ToList().AsReadOnly();

			// Track search query synchronously to ensure it's saved
			await TrackSearchQueryAsync(q.Trim());

			return Ok(new ServiceResponse<IReadOnlyList<ProductSummaryDto>>(true, "Search completed", results));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error searching products with query: {Query}", q);
			return BadRequest(new ServiceResponse<IReadOnlyList<ProductSummaryDto>>(false, $"Error: {ex.Message}"));
		}
	}

	/// <summary>
	/// Отримати популярні пошукові запити
	/// </summary>
	[HttpGet("popular")]
	[AllowAnonymous]
	[OutputCache(Duration = 300)] // Cache for 5 minutes
	public async Task<IActionResult> GetPopular([FromQuery] int limit = 10)
	{
		try
		{
			using var scope = _serviceScopeFactory.CreateScope();
			var searchQueryRepository = scope.ServiceProvider.GetRequiredService<ISearchQueryRepository>();

			limit = Math.Clamp(limit, 1, 20);

			var queries = await searchQueryRepository.GetPopularAsync(limit);
			var results = queries.Select(q => new PopularQueryDto(q.Query, q.SearchCount)).ToList().AsReadOnly();

			return Ok(new ServiceResponse<IReadOnlyList<PopularQueryDto>>(true, "Popular queries retrieved", results));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving popular queries");
			return BadRequest(new ServiceResponse<IReadOnlyList<PopularQueryDto>>(false, $"Error: {ex.Message}"));
		}
	}

	private async Task TrackSearchQueryAsync(string query)
	{
		try
		{
			using var scope = _serviceScopeFactory.CreateScope();
			var searchQueryRepository = scope.ServiceProvider.GetRequiredService<ISearchQueryRepository>();
			var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

			var normalized = query.ToLowerInvariant();
			var existing = await searchQueryRepository.GetByQueryAsync(normalized);

			if (existing != null)
			{
				existing.IncrementCount();
				searchQueryRepository.Update(existing);
			}
			else
			{
				var newQuery = SearchQuery.Create(query);
				searchQueryRepository.Add(newQuery);
			}

			await unitOfWork.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			// Log but don't throw - tracking failures shouldn't break search
			_logger.LogWarning(ex, "Failed to track search query: {Query}", query);
		}
	}
}

/// <summary>
/// DTO для популярних пошукових запитів
/// </summary>
public sealed record PopularQueryDto(string Query, long SearchCount);
