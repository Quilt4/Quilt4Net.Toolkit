namespace Quilt4Net.Toolkit.Features.Measure;

public static class LogDataExtensions
{
    public static Dictionary<string, object> GetData(this LogData logData)
    {
        var data = logData?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object>();
        return data;
    }

    public static TLogData AddField<TLogData, T>(this TLogData logData, string key, T data)
        where TLogData : LogData
    {
        logData.AddData(key, data);
        return logData;
    }

    public static TLogData AddFields<TLogData>(this TLogData logData, LogData data)
        where TLogData : LogData
    {
        if (data == null) return logData;

        foreach (var item in data)
        {
            logData.AddData(item.Key, item.Value);
        }
        return logData;
    }
}