using System.Collections;

namespace Quilt4Net.Toolkit.Features.Measure;

public class LogData : IEnumerable<KeyValuePair<string, object>>
{
    private readonly Dictionary<string, object> _data = new();

    public LogData()
    {
    }

    public LogData(Dictionary<string, object> dictionary)
    {
        _data = dictionary;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    //public static LogData Build(params KeyValuePair<string, object>[] items)
    //{
    //    return new LogData();
    //}

    //public static LogData Build(IDictionary dictionary)
    //{
    //    return new LogData();
    //}

    internal void AddData<T>(string key, T data)
    {
        _data.TryAdd(key, data);
    }
}