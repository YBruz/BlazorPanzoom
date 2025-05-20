using System.Diagnostics.CodeAnalysis;
using BlazorPanzoom.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorPanzoom.Services;

[ExcludeFromCodeCoverage]
public static class ServiceExtensions
{
	public static IServiceCollection AddBlazorPanzoomServices(this IServiceCollection services)
	{
		return services
			.AddJSBlazorPanzoomInterop()
			.AddPanzoomHelper();
	}

	private static IServiceCollection AddJSBlazorPanzoomInterop(this IServiceCollection services)
	{
		services.TryAddScoped<IJSBlazorPanzoomInterop, JSBlazorPanzoomInterop>();
		return services;
	}

	private static IServiceCollection AddPanzoomHelper(this IServiceCollection services)
	{
		services.TryAddScoped<IPanzoomHelper, PanzoomHelper>();
		return services;
	}


}