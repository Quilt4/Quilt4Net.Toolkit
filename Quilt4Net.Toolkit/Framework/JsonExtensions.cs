using System.Text.Json;

namespace Quilt4Net.Toolkit.Framework;

public static class JsonExtensions
{
    public static string FormatJson(this string rawJson)
    {
        try
        {
            // Parse and reformat the JSON
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawJson);
            return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            // If parsing fails, return the original string
            return rawJson;
        }
    }
}