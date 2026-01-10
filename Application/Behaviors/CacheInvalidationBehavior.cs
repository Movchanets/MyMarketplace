using Application.DTOs;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically invalidates cache tags
/// after a command that implements ICacheInvalidatingCommand completes successfully.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
	where TRequest : notnull
{
	private readonly ICacheInvalidationService? _cacheInvalidation;
	private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

	public CacheInvalidationBehavior(
		ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger,
		ICacheInvalidationService? cacheInvalidation = null)
	{
		_logger = logger;
		_cacheInvalidation = cacheInvalidation;
	}

	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		var response = await next();

		// Only invalidate cache if:
		// 1. The request implements ICacheInvalidatingCommand
		// 2. The cache invalidation service is registered
		// 3. The response indicates success (for ServiceResponse types)
		if (request is ICacheInvalidatingCommand invalidatingCommand && _cacheInvalidation is not null)
		{
			// Check if response is ServiceResponse and if it was successful
			var shouldInvalidate = true;

			if (response is ServiceResponse serviceResponse)
			{
				shouldInvalidate = serviceResponse.IsSuccess;
			}
			else if (response is not null)
			{
				// For generic ServiceResponse<T>, check IsSuccess via reflection
				var responseType = response.GetType();
				if (responseType.IsGenericType &&
					responseType.GetGenericTypeDefinition() == typeof(ServiceResponse<>))
				{
					var isSuccessProperty = responseType.GetProperty(nameof(ServiceResponse.IsSuccess));
					if (isSuccessProperty is not null)
					{
						shouldInvalidate = (bool)(isSuccessProperty.GetValue(response) ?? false);
					}
				}
			}

			if (shouldInvalidate)
			{
				var tags = invalidatingCommand.CacheTags.ToList();
				if (tags.Count > 0)
				{
					_logger.LogInformation(
						"Invalidating cache tags {Tags} after successful command {CommandType}",
						string.Join(", ", tags),
						typeof(TRequest).Name);

					await _cacheInvalidation.InvalidateByTagsAsync(tags, cancellationToken);
				}
			}
		}

		return response;
	}
}
