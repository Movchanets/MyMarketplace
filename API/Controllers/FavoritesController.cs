using Application.Commands.Favorite.AddToFavorites;
using Application.Commands.Favorite.MergeGuestFavorites;
using Application.Commands.Favorite.RemoveFromFavorites;
using Application.DTOs;
using Application.Interfaces;
using Application.Queries.Favorites.GetUserFavorites;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

/// <summary>
/// API для управління обраними товарами
/// </summary>
[ApiController]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(
        IMediator mediator,
        IUserRepository userRepository,
        ILogger<FavoritesController> logger)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Отримати список обраних товарів користувача
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFavorites()
    {
        try
        {
            var identityUserId = GetUserId();
            _logger.LogInformation("GetFavorites called for identity user ID: {UserId}", identityUserId);
            if (!identityUserId.HasValue)
            {
                _logger.LogWarning("User not authenticated - no valid user ID found");
                return Unauthorized(new ServiceResponse<IReadOnlyList<FavoriteProductDto>>(false, "User not authenticated"));
            }

            // Get the domain user
            var domainUser = await _userRepository.GetByIdentityUserIdAsync(identityUserId.Value);
            if (domainUser is null)
            {
                _logger.LogWarning("Domain user not found for identity user {IdentityUserId}", identityUserId);
                return BadRequest(new ServiceResponse<IReadOnlyList<FavoriteProductDto>>(false, "User profile not found"));
            }

            var query = new GetUserFavoritesQuery(domainUser.Id);
            var result = await _mediator.Send(query);

            _logger.LogInformation("GetUserFavoritesQuery returned {Count} favorites for domain user {DomainUserId}", result.Payload?.Count ?? 0, domainUser.Id);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("GetUserFavoritesQuery failed: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorites");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<FavoriteProductDto>>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Додати товар до обраних
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToFavorites([FromBody] AddToFavoritesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request data for AddToFavorites");
                return BadRequest(new ServiceResponse<bool>(false, "Invalid request data"));
            }

            var userId = GetUserId();
            _logger.LogInformation("AddToFavorites called for user ID: {UserId}, product: {ProductId}", userId, request.ProductId);
            if (!userId.HasValue)
            {
                _logger.LogWarning("User not authenticated for AddToFavorites");
                return Unauthorized(new ServiceResponse<bool>(false, "User not authenticated"));
            }

            var command = new AddToFavoritesCommand(userId.Value, request.ProductId);
            var result = await _mediator.Send(command);

            _logger.LogInformation("AddToFavoritesCommand result: Success={Success}, Message={Message}", result.IsSuccess, result.Message);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to favorites for product {ProductId}", request.ProductId);
            return StatusCode(500, new ServiceResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Видалити товар з обраних
    /// </summary>
    [HttpDelete("{productId}")]
    [Authorize]
    public async Task<IActionResult> RemoveFromFavorites(Guid productId)
    {
        try
        {
            var identityUserId = GetUserId();
            if (!identityUserId.HasValue)
            {
                return Unauthorized(new ServiceResponse<bool>(false, "User not authenticated"));
            }

            // Pass identity user ID - the handler will look up the domain user
            var command = new RemoveFromFavoritesCommand(identityUserId.Value, productId);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from favorites for product {ProductId}", productId);
            return StatusCode(500, new ServiceResponse<bool>(false, "Internal server error"));
        }
    }

    /// <summary>
    /// Об'єднати гостьові обрані з обліковим записом користувача
    /// </summary>
    [HttpPost("merge-guest")]
    [Authorize]
    public async Task<IActionResult> MergeGuestFavorites([FromBody] MergeGuestFavoritesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<int>(false, "Invalid request data"));
            }

            var identityUserId = GetUserId();
            if (!identityUserId.HasValue)
            {
                return Unauthorized(new ServiceResponse<int>(false, "User not authenticated"));
            }

            // Pass identity user ID - the handler will look up the domain user
            var command = new MergeGuestFavoritesCommand(identityUserId.Value, request.ProductIds);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging guest favorites for user");
            return StatusCode(500, new ServiceResponse<int>(false, "Internal server error"));
        }
    }

    private Guid? GetUserId()
    {
        // Try different claim types that might contain the user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}