using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Features.FeatureToggle;
using Quilt4Net.Toolkit.Features.Health;
using Quilt4Net.Toolkit.Framework;

var builder = Host.CreateApplicationBuilder(args);

builder.AddQuilt4NetRemoteConfiguration();
builder.AddQuilt4NetHealthClient(o =>
{
    o.HealthAddress = "https://localhost:7119";
});

var host = builder.Build();

var toggleService = host.Services.GetRequiredService<IFeatureToggleService>();
var configService = host.Services.GetRequiredService<IRemoteConfigurationService>();
var healthClient = host.Services.GetRequiredService<IHealthClient>();

// Feature toggles
Console.WriteLine("=== Feature Toggles ===");
var darkMode = await toggleService.GetToggleAsync("dark-mode", fallback: false);
Console.WriteLine($"dark-mode: {darkMode}");

var betaFeatures = await toggleService.GetToggleAsync("beta-features", fallback: true);
Console.WriteLine($"beta-features: {betaFeatures}");

// Remote configuration
Console.WriteLine();
Console.WriteLine("=== Remote Configuration ===");
var maxRetries = await configService.GetValueAsync("max-retries", fallback: 3);
Console.WriteLine($"max-retries: {maxRetries}");

var welcomeMessage = await configService.GetValueAsync("welcome-message", fallback: "Hello!");
Console.WriteLine($"welcome-message: {welcomeMessage}");

// Health client
Console.WriteLine();
Console.WriteLine("=== Health Client ===");
try
{
    var live = await healthClient.GetLiveAsync(CancellationToken.None);
    Console.WriteLine($"Live: {live.Status}");

    var ready = await healthClient.GetReadyAsync(CancellationToken.None);
    Console.WriteLine($"Ready: {ready.Status}");

    var version = await healthClient.GetVersionAsync(CancellationToken.None);
    Console.WriteLine($"Version: {version.Version}");
}
catch (Exception ex)
{
    Console.WriteLine($"Health check unavailable: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("Done. Press any key to exit.");
Console.ReadKey();
