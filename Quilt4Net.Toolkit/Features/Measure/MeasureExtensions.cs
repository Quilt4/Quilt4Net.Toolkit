using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Features.Measure;

public static class MeasureExtensions
{
    public static void Measure(this ILogger logger, Action func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        logger.MeasureAsync(null, _ =>
        {
            func.Invoke();
            return Task.FromResult(true);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static void Measure(this ILogger logger, Action<MeasurementLogData> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        logger.MeasureAsync(null, d =>
        {
            func.Invoke(d);
            return Task.FromResult(true);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static void Measure(this ILogger logger, string action, Action func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        logger.MeasureAsync(action, _ =>
        {
            func.Invoke();
            return Task.FromResult(true);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static void Measure(this ILogger logger, string action, Action<MeasurementLogData> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        logger.MeasureAsync(action, d =>
        {
            func.Invoke(d);
            return Task.FromResult(true);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static T Measure<T>(this ILogger logger, Func<T> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(null, _ =>
        {
            var result = func.Invoke();
            return Task.FromResult(result);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static T Measure<T>(this ILogger logger, Func<MeasurementLogData, T> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(null, d =>
        {
            var result = func.Invoke(d);
            return Task.FromResult(result);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static T Measure<T>(this ILogger logger, string action, Func<T> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(action, _ =>
        {
            var result = func.Invoke();
            return Task.FromResult(result);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static T Measure<T>(this ILogger logger, string action, Func<MeasurementLogData, T> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(action, d =>
        {
            var result = func.Invoke(d);
            return Task.FromResult(result);
        }, level, logData).GetAwaiter().GetResult();
    }

    public static Task MeasureAsync(this ILogger logger, Func<Task> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(null, async _ =>
        {
            await func();
            return true;
        }, level, logData);
    }

    public static Task MeasureAsync(this ILogger logger, Func<MeasurementLogData, Task> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(null, async d =>
        {
            await func(d);
            return true;
        }, level, logData);
    }

    public static Task MeasureAsync(this ILogger logger, string action, Func<Task> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(action, async _ =>
        {
            await func();
            return true;
        }, level, logData);
    }

    public static Task MeasureAsync(this ILogger logger, string action, Func<MeasurementLogData, Task> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(action, async d =>
        {
            await func(d);
            return true;
        }, level, logData);
    }

    public static Task<T> MeasureAsync<T>(this ILogger logger, Func<Task<T>> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(null, async _ => await func(), level, logData);
    }

    public static Task<T> MeasureAsync<T>(this ILogger logger, Func<MeasurementLogData, Task<T>> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync(null, func, level, logData);
    }

    public static Task<T> MeasureAsync<T>(this ILogger logger, string action, Func<Task<T>> func, LogLevel level = LogLevel.Information, LogData logData = null)
    {
        return logger.MeasureAsync<T>(action, async _ => await func.Invoke(), level, logData);
    }

    public static async Task<T> MeasureAsync<T>(this ILogger logger, string action, Func<MeasurementLogData, Task<T>> func, LogLevel logLevel = LogLevel.Information, LogData logData = null)
    {
        action ??= "Action";

        var sw = Stopwatch.StartNew();

        var data = new MeasurementLogData(logLevel, sw)
            .AddFields(logData)
            .AddField("Monitor", Constants.Monitor)
            .AddField("Method", "Measure");

        T result;
        try
        {
            result = await func(data);
        }
        catch (Exception e)
        {
            var d = data.Concat(e.GetData()).ToUniqueDictionary();
            var details = System.Text.Json.JsonSerializer.Serialize(d);
            logger.LogError("Measured {Action} in {Elapsed} ms, failed {ErrorMessage} @{StackTrace}. {Details}", action, sw.Elapsed, e.Message, e.StackTrace, details);
            throw;
        }
        sw.Stop();

        if (!data.Omit)
        {
            var details = System.Text.Json.JsonSerializer.Serialize(data.GetData().ToUniqueDictionary());
            logger.Log(data.LogLevel, "Measured {Action} in {Elapsed} ms. {Details}", action, sw.Elapsed, details);
        }

        return result;
    }

    public static void Elapsed(this ILogger logger, string action, TimeSpan elapsed, LogLevel logLevel = LogLevel.Information, LogData logData = null)
    {
        action ??= "Action";

        var data = logData ?? new LogData();
        data.AddField("Monitor", Constants.Monitor);
        data.AddField("Method", "Measure");
        //data.AddData("Elapsed", elapsed);

        var details = System.Text.Json.JsonSerializer.Serialize(data.GetData().ToUniqueDictionary());
        logger.Log(logLevel, "Measured {Action} in {Elapsed} ms. {Details}", action, elapsed, details);
    }

    public static void Count(this ILogger logger, string action, int count, LogLevel logLevel = LogLevel.Information, LogData logData = null)
    {
        action ??= "Action";

        var data = logData ?? new LogData();
        data.AddField("Monitor", Constants.Monitor);
        data.AddField("Method", "Count");
        //data.AddData("Count", count);

        var details = System.Text.Json.JsonSerializer.Serialize(data.GetData().ToUniqueDictionary());
        logger.Log(logLevel, "Count {Action} as {Count}. {Details}", action, count, details);
    }
}