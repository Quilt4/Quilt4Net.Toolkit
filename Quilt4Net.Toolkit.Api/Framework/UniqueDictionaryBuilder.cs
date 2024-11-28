using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Framework;

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
}