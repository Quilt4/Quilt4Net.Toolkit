namespace Quilt4Net.Toolkit.Features.Measure;

public static class ExceptionExtension
{
    public static T AddData<T>(this T item, object key, object value)
        where T : Exception
    {
        if (item.Data.Contains(key)) item.Data.Remove(key);
        item.Data.Add(key, value);
        return item;
    }

    public static T AddData<T>(this T item, LogData data)
        where T : Exception
    {
        foreach (var dataItem in data.GetData())
        {
            item.AddData(dataItem.Key, dataItem.Value);
        }
        return item;
    }

    public static IEnumerable<KeyValuePair<string, object>> GetData(this Exception e)
    {
        foreach (var key in e.Data.Keys)
        {
            if (key is not null)
            {
                yield return new KeyValuePair<string, object>($"{key}", e.Data[key]);
            }
        }
    }

    public static IDictionary<string, object> ToDictionary(this Exception e)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (System.Collections.DictionaryEntry entry in e.Data)
        {
            dictionary.TryAdd($"{entry.Key}", entry.Value);
        }

        return dictionary;
    }
}