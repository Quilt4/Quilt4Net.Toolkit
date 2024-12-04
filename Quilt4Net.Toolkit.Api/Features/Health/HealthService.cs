using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Api.Features.Probe;
using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Health;

internal class HealthService : IHealthService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostedServiceProbeRegistry _hostedServiceProbeRegistry;
    private readonly Quilt4NetApiOptions _option;
    private readonly ILogger<HealthService> _logger;

    public HealthService(IHostEnvironment hostEnvironment, IServiceProvider serviceProvider, IHostedServiceProbeRegistry hostedServiceProbeRegistry, Quilt4NetApiOptions option, ILogger<HealthService> logger = default)
    {
        _hostEnvironment = hostEnvironment;
        _serviceProvider = serviceProvider;
        _hostedServiceProbeRegistry = hostedServiceProbeRegistry;
        _option = option;
        _logger = logger;
    }

    public async IAsyncEnumerable<KeyValuePair<string, HealthComponent>> GetStatusAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var probe in _hostedServiceProbeRegistry.GetProbesAsync().WithCancellation(cancellationToken))
        {
            yield return probe;
        }

        var tasksFromServices = _option.ComponentServices.SelectMany(x => ((IComponentService)_serviceProvider.GetService(x))?.GetComponents()).Select(x => RunTaskAsync(x.Name, x.Essential, x.CheckAsync));
        var tasksFromAdd = _option.Components.Select(x => RunTaskAsync(x.Name, x.Essential, x.CheckAsync));
        var taskList = tasksFromServices.Union(tasksFromAdd).ToList();

        while (taskList.Count > 0)
        {
            var completedTask = await Task.WhenAny(taskList); // Get the first task that completes
            yield return BuildResponse(completedTask); // Return the completed task
            taskList.Remove(completedTask); // Remove the completed task from the list
        }
    }

    private KeyValuePair<string, HealthComponent> BuildResponse(Task<RunTaskResult> x)
    {
        var result = new KeyValuePair<string, HealthComponent>(x.Result.Name, new HealthComponent
        {
            Status = BuildStatus(x.Result.Result.Success, x.Result.Essential),
            Details = new Dictionary<string, string>
            {
                { "elapsed", $"{x.Result.Elapsed}" },
            }
        });

        if (!string.IsNullOrEmpty(x.Result.Result.Message))
        {
            result.Value.Details.TryAdd("message", x.Result.Result.Message);
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

    private async Task<RunTaskResult> RunTaskAsync(string name, bool essential, Func<IServiceProvider, Task<CheckResult>> check)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrEmpty(name)) name = "Component";

        try
        {
            _logger?.LogTrace("Starting check for {name} component.", name);
            var status = await check.Invoke(_serviceProvider);
            stopwatch.Stop();
            _logger?.LogTrace("Complete check for {name} component after {elapsed}.", name, stopwatch.Elapsed);
            return new RunTaskResult { Name = name, Essential = essential, Result = status, Elapsed = stopwatch.Elapsed };
        }
        catch (Exception exception)
        {
            Guid? correlationId = null;
            if (_logger != null)
            {
                correlationId = Guid.NewGuid();
                _logger.LogError("Failed check for {name} component after {elapsed}. {message} [CorrelationId: {correlationId}]", name, stopwatch.Elapsed, exception.Message, correlationId);
            }

            return new RunTaskResult { Name = name, Essential = essential, Result = new CheckResult { Success = false }, Elapsed = stopwatch.Elapsed, Exception = exception, CorrelationId = correlationId };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private record RunTaskResult
    {
        public required string Name { get; init; }
        public required bool Essential { get; init; }
        public required CheckResult Result { get; init; }
        public required TimeSpan Elapsed { get; init; }
        public Exception Exception { get; init; }
        public Guid? CorrelationId { get; init; }
    }
}