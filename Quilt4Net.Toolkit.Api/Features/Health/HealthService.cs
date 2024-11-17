using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Api.Features.Health;

internal class HealthService : IHealthService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Quilt4NetApiOptions _option;
    private readonly ILogger<HealthService> _logger;

    public HealthService(IServiceProvider serviceProvider, Quilt4NetApiOptions option, ILogger<HealthService> logger = default)
    {
        _serviceProvider = serviceProvider;
        _option = option;
        _logger = logger;
    }

    public Task<HealthResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        var tasks = _option.Components.Select(x => RunTaskAsync(x.Name, x.CheckAsync)).ToArray();

        Task.WaitAll(tasks.ToArray<Task>(), cancellationToken);
        var components = tasks.Select(x =>
        {
            var result = new KeyValuePair<string, Component>(x.Result.Name, new Component
            {
                Status = $"{x.Result.Status}",
                Details = new Dictionary<string, string>
                {
                    { "elapsed", $"{x.Result.Elapsed}" },
                }
            });

            if (x.Result.Exception != null)
            {
                //TODO: Detail level should be configurable
                result.Value.Details.TryAdd("exception.message", x.Result.Exception.Message);
                result.Value.Details.TryAdd("exception.stacktrace", x.Result.Exception.StackTrace);
            }

            return result;
        }).ToArray();

        var status = components.Any()
            ? components.Max(x => Enum.Parse<HealthStatusResult>(x.Value.Status, true))
            : HealthStatusResult.Healthy;

        return Task.FromResult(new HealthResponse
        {
            Status = status,
            Components = components.ToDictionary(x => x.Key, x => x.Value)
        });
    }

    private async Task<(string Name, HealthStatusResult Status, TimeSpan Elapsed, Exception Exception)> RunTaskAsync(string name, Func<IServiceProvider, Task<HealthStatusResult>> check)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger?.LogTrace("Starting check for {name} component.", name);
            var result = await check.Invoke(_serviceProvider);
            stopwatch.Stop();
            _logger?.LogTrace("Complete check for {name} component after {elapsed}.", name, stopwatch.Elapsed);
            return (name, result, stopwatch.Elapsed, null);
        }
        catch (Exception exception)
        {
            _logger?.LogError("Failed check for {name} component after {elapsed}. {message}", name, stopwatch.Elapsed, exception.Message);
            return (name, HealthStatusResult.Unhealthy, stopwatch.Elapsed, exception);
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}