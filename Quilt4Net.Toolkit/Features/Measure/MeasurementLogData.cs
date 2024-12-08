using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Quilt4Net.Toolkit.Features.Measure;

public class MeasurementLogData : LogData
{
    private readonly Stopwatch _stopwatch;

    internal MeasurementLogData(LogLevel logLevel, Stopwatch stopwatch)
    {
        _stopwatch = stopwatch;
        LogLevel = logLevel;
    }

    public LogLevel LogLevel { get; private set; }
    public bool Omit { get; private set; }

    public void SetLogLevel(LogLevel logLevel)
    {
        LogLevel = logLevel;
    }

    public void SetOmit()
    {
        Omit = true;
    }

    public TimeSpan GetElapsed()
    {
        return _stopwatch.Elapsed;
    }
}