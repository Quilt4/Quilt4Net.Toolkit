using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var tasks = _option.Components.Select(x => RunTaskAsync(x.Name, x.Essential, x.CheckAsync)).ToArray();

        Task.WaitAll(tasks.ToArray<Task>(), cancellationToken);
        var components = tasks.Select(x =>
        {
            var result = new KeyValuePair<string, Component>(x.Result.Name, new Component
            {
                Status = BuildStatus(x.Result.Status, x.Result.Essential),
                Details = new Dictionary<string, string>
                {
                    { "elapsed", $"{x.Result.Elapsed}" },
                }
            });

            if (x.Result.Exception != null)
            {
                var correlationIdMessage = BuildCorrelationIdMessage(x.Result.CorrelationId);
                var exceptionDataLevel = _option.ExceptionDataLevel ?? GetDefaultExceptionLevel();
                switch (exceptionDataLevel)
                {
                    case ExceptionDataLevel.Hidden:
                        result.Value.Details.TryAdd("exception.message", $"Hidden exception. {correlationIdMessage}");
                        break;
                    case ExceptionDataLevel.Message:
                        result.Value.Details.TryAdd("exception.message", $"{x.Result.Exception.Message} {correlationIdMessage}");
                        break;
                    case ExceptionDataLevel.StackTrace:
                        result.Value.Details.TryAdd("exception.message", $"{x.Result.Exception.Message} {correlationIdMessage}");
                        result.Value.Details.TryAdd("exception.stacktrace", x.Result.Exception.StackTrace);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_option.ExceptionDataLevel), _option.ExceptionDataLevel, null);
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
            Components = components.ToDictionary(x => x.Key, x => x.Value)
        });
    }

    private ExceptionDataLevel? GetDefaultExceptionLevel()
    {
        if (_hostEnvironment.IsProduction()) return ExceptionDataLevel.Hidden;
        if (_hostEnvironment.IsDevelopment()) return ExceptionDataLevel.StackTrace;
        return ExceptionDataLevel.Message;
    }

    private static string BuildCorrelationIdMessage(Guid? correlationId)
    {
        if (correlationId == null)
        {
            return "This message has not been logged.";
        }

        return $"Logged with correlationId {correlationId}";
    }

    private static HealthStatus BuildStatus(CheckResult checkResult, bool essential)
    {
        if (checkResult.Success) return HealthStatus.Healthy;

        if (!essential) return HealthStatus.Degraded;

        return HealthStatus.Unhealthy;
    }

    private async Task<(string Name, bool Essential, CheckResult Status, TimeSpan Elapsed, Exception Exception, Guid? CorrelationId)> RunTaskAsync(string name, bool essential, Func<IServiceProvider, Task<CheckResult>> check)
    {
        var stopwatch = Stopwatch.StartNew();

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