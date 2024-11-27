using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Health;

internal class HealthService : IHealthService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IServiceProvider _serviceProvider;
    private readonly Quilt4NetApiOptions _option;
    private readonly ILogger<HealthService> _logger;

    public HealthService(IHostEnvironment hostEnvironment, IServiceProvider serviceProvider, Quilt4NetApiOptions option, ILogger<HealthService> logger = default)
    {
        _hostEnvironment = hostEnvironment;
        _serviceProvider = serviceProvider;
        _option = option;
        _logger = logger;
    }

    public Task<HealthResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        var tasksFromServices = _option.ComponentServices.SelectMany(x => ((IComponentService)_serviceProvider.GetService(x))?.GetComponents()).Select(x => RunTaskAsync(x.Name, x.Essential, x.CheckAsync));
        var tasksFromAdd = _option.Components.Select(x => RunTaskAsync(x.Name, x.Essential, x.CheckAsync));
        var tasks = tasksFromServices.Union(tasksFromAdd).ToArray();

        Task.WaitAll(tasks.ToArray<Task>(), cancellationToken);
        var components = tasks.Select(x =>
        {
            var result = new KeyValuePair<string, HealthComponent>(x.Result.Name, new HealthComponent
            {
                Status = BuildStatus(x.Result.Status.Success, x.Result.Essential),
                Details = new Dictionary<string, string>
                {
                    { "elapsed", $"{x.Result.Elapsed}" },
                }
            });

            if (!string.IsNullOrEmpty(x.Result.Status.Message))
            {
                result.Value.Details.TryAdd("message", x.Result.Status.Message);
            }

            if (x.Result.Exception != null)
            {
                var correlationIdMessage = BuildCorrelationIdMessage(x.Result.CorrelationId);
                var exceptionDataLevel = _option.ExceptionDetail ?? GetDefaultExceptionLevel();
                switch (exceptionDataLevel)
                {
                    case ExceptionDetailLevel.Hidden:
                        result.Value.Details.TryAdd("exception.message", $"Hidden exception. {correlationIdMessage}");
                        break;
                    case ExceptionDetailLevel.Message:
                        result.Value.Details.TryAdd("exception.message", $"{x.Result.Exception.Message} {correlationIdMessage}");
                        break;
                    case ExceptionDetailLevel.StackTrace:
                        result.Value.Details.TryAdd("exception.message", $"{x.Result.Exception.Message} {correlationIdMessage}");
                        result.Value.Details.TryAdd("exception.stacktrace", x.Result.Exception.StackTrace);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_option.ExceptionDetail), _option.ExceptionDetail, null);
                }
            }

            return result;
        }).ToArray();

        var status = components.Any()
            ? components.Max(x => x.Value.Status)
            : HealthStatus.Healthy;

        return Task.FromResult(new HealthResponse
        {
            Status = status,
            Components = components.ToUniqueDictionary()
        });
    }

    private ExceptionDetailLevel? GetDefaultExceptionLevel()
    {
        if (_hostEnvironment.IsProduction()) return ExceptionDetailLevel.Hidden;
        if (_hostEnvironment.IsDevelopment()) return ExceptionDetailLevel.StackTrace;
        return ExceptionDetailLevel.Message;
    }

    private static string BuildCorrelationIdMessage(Guid? correlationId)
    {
        if (correlationId == null)
        {
            return "This message has not been logged.";
        }

        return $"Logged with correlationId {correlationId}";
    }

    private static HealthStatus BuildStatus(bool success, bool essential)
    {
        if (success) return HealthStatus.Healthy;

        if (!essential) return HealthStatus.Degraded;

        return HealthStatus.Unhealthy;
    }

    private async Task<(string Name, bool Essential, CheckResult Status, TimeSpan Elapsed, Exception Exception, Guid? CorrelationId)> RunTaskAsync(string name, bool essential, Func<IServiceProvider, Task<CheckResult>> check)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrEmpty(name)) name = "Component";

        try
        {
            _logger?.LogTrace("Starting check for {name} component.", name);
            var result = await check.Invoke(_serviceProvider);
            stopwatch.Stop();
            _logger?.LogTrace("Complete check for {name} component after {elapsed}.", name, stopwatch.Elapsed);
            return (name, essential, result, stopwatch.Elapsed, null, null);
        }
        catch (Exception exception)
        {
            Guid? correlationId = null;
            if (_logger != null)
            {
                correlationId = Guid.NewGuid();
                _logger.LogError("Failed check for {name} component after {elapsed}. {message} [CorrelationId: {correlationId}]", name, stopwatch.Elapsed, exception.Message, correlationId);
            }

            return (name, essential, new CheckResult { Success = false }, stopwatch.Elapsed, exception, correlationId);
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}

internal static class UniqueDictionaryBuilder
{
    public static Dictionary<string, HealthComponent> ToUniqueDictionary(this KeyValuePair<string, HealthComponent>[] components)
    {
        var result = new Dictionary<string, HealthComponent>();
        var keyCounts = new Dictionary<string, int>(); // Track occurrences of each key

        foreach (var component in components)
        {
            var key = component.Key;

            keyCounts.TryAdd(key, 0);
            keyCounts[key]++;

            // Append suffix if there are duplicates
            if (keyCounts[key] == 1 && components.Count(c => c.Key == key) > 1)
            {
                key = $"{key}.0"; // First duplicate occurrence gets .0
            }
            else if (keyCounts[key] > 1)
            {
                key = $"{key}.{keyCounts[key] - 1}"; // Subsequent occurrences get .1, .2, etc.
            }

            result.Add(key, component.Value);
        }

        return result;
    }


    //public static Dictionary<string, HealthComponent> ToUniqueDictionary(this KeyValuePair<string, HealthComponent>[] components)
    //{
    //    var result = new Dictionary<string, HealthComponent>();
    //    var keyCounters = new Dictionary<string, int>(); // Track counters for each key

    //    foreach (var component in components)
    //    {
    //        var key = component.Key ?? "Component";

    //        if (result.ContainsKey(key))
    //        {
    //            keyCounters.TryAdd(key, 1);

    //            keyCounters[key]++;
    //            key = $"{key}.{keyCounters[key] - 1}"; // Append suffix
    //        }

    //        result.Add(key, component.Value);
    //    }

    //    return result;
    //}
}