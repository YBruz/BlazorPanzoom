using System;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorPanzoom.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorPanzoom.Demo;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebAssemblyHostBuilder.CreateDefault(args);
		builder.RootComponents.Add<App>("#app");

		builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

		builder.Services.AddBlazorPanzoomServices();

		await builder.Build().RunAsync();
	}
}