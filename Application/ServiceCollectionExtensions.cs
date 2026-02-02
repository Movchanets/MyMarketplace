using Application.Interfaces;
using Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		// Register all FluentValidation validators from this assembly
		services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

		// Register MediatR pipeline behavior for validation
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.ValidationBehavior<,>));

		// Register MediatR pipeline behavior for cache invalidation
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Behaviors.CacheInvalidationBehavior<,>));

		// Register application services
		services.AddScoped<ICartService, CartService>();

		return services;
	}
}
