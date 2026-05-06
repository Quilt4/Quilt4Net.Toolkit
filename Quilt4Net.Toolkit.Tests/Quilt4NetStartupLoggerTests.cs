using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Features.Logging;
using Xunit;

namespace Quilt4Net.Toolkit.Tests;

public class Quilt4NetStartupLoggerTests
{
    [Fact]
    public void Log_emits_information_entry_with_application_details()
    {
        var capture = new CapturingLogger();
        var options = new Quilt4NetLoggingOptions
        {
            ApplicationName = "florida-server",
            Version = "1.4.7",
            Environment = "Production"
        };

        Quilt4NetStartupLogger.Log(capture, options);

        capture.Entries.Should().ContainSingle();
        var entry = capture.Entries[0];
        entry.Level.Should().Be(LogLevel.Information);
        entry.FormattedMessage.Should().Contain("florida-server");
        entry.FormattedMessage.Should().Contain("1.4.7");
        entry.FormattedMessage.Should().Contain("Production");
    }

    [Fact]
    public void Log_attaches_Quilt4NetStartup_marker_in_scope()
    {
        var capture = new CapturingLogger();
        var options = new Quilt4NetLoggingOptions { ApplicationName = "x" };

        Quilt4NetStartupLogger.Log(capture, options);

        capture.Entries.Should().ContainSingle();
        capture.Entries[0].Scopes.Should().ContainSingle()
            .Which.Should().BeAssignableTo<IDictionary<string, object>>()
            .Which["Quilt4NetStartup"].Should().Be("true");
    }

    [Fact]
    public void Log_handles_null_option_values_with_unknown_placeholder()
    {
        var capture = new CapturingLogger();
        var options = new Quilt4NetLoggingOptions();

        Quilt4NetStartupLogger.Log(capture, options);

        capture.Entries[0].FormattedMessage.Should().Contain("(unknown)");
    }

    [Fact]
    public async Task Hosted_service_emits_log_on_StartAsync()
    {
        var capture = new CapturingLogger<Quilt4NetStartupHostedService>();
        var options = new Quilt4NetLoggingOptions { ApplicationName = "from-host" };
        var service = new Quilt4NetStartupHostedService(capture, options);

        await service.StartAsync(CancellationToken.None);

        capture.Inner.Entries.Should().ContainSingle()
            .Which.FormattedMessage.Should().Contain("from-host");
    }

    [Fact]
    public void LogQuilt4NetStartup_extension_resolves_logger_from_provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Quilt4NetLoggingOptions { ApplicationName = "wpf-app", Version = "2.0.0", Environment = "Production" });
        var capture = new CapturingLogger();
        services.AddSingleton<ILoggerFactory>(new CapturingLoggerFactory(capture));
        var provider = services.BuildServiceProvider();

        provider.LogQuilt4NetStartup();

        capture.Entries.Should().ContainSingle()
            .Which.FormattedMessage.Should().Contain("wpf-app");
    }

    [Fact]
    public void Hosted_service_is_registered_by_AddQuilt4NetLogging()
    {
        var services = new ServiceCollection();
        services.AddQuilt4NetLogging(applicationName: "x");

        services.Should().Contain(d => d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(Quilt4NetStartupHostedService));
    }

    private sealed record LogEntry(LogLevel Level, string FormattedMessage, List<object> Scopes);

    private sealed class CapturingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = new();
        private readonly List<object> _activeScopes = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            _activeScopes.Add(state);
            return new Pop(() => _activeScopes.RemoveAt(_activeScopes.Count - 1));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), new List<object>(_activeScopes)));
        }

        private sealed class Pop : IDisposable
        {
            private readonly Action _action;
            public Pop(Action action) { _action = action; }
            public void Dispose() => _action();
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public CapturingLogger Inner { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => Inner.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => Inner.Log(logLevel, eventId, state, exception, formatter);
    }

    private sealed class CapturingLoggerFactory : ILoggerFactory
    {
        private readonly CapturingLogger _logger;
        public CapturingLoggerFactory(CapturingLogger logger) { _logger = logger; }
        public ILogger CreateLogger(string categoryName) => _logger;
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }
}
