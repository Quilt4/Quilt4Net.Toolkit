using System.Text;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public interface IApplicationInsightsContext
{
    public string TenantId { get; }
    public string WorkspaceId { get; }
    public string ClientId { get; }
    public string ClientSecret { get; }
}

public static class ApplicationInsightsContextExtensions
{
    public static string ToKey(this IApplicationInsightsContext context)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(context);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        return base64;
    }

    public static IApplicationInsightsContext ToApplicationInsightsContext(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        var decodedBytes = Convert.FromBase64String(key);
        var decoded = Encoding.UTF8.GetString(decodedBytes);
        var context = System.Text.Json.JsonSerializer.Deserialize<Ctx>(decoded);
        return context;
    }

    internal record Ctx : IApplicationInsightsContext
    {
        public required string TenantId { get; init; }
        public required string WorkspaceId { get; init; }
        public required string ClientId { get; init; }
        public required string ClientSecret { get; init; }
    }
}