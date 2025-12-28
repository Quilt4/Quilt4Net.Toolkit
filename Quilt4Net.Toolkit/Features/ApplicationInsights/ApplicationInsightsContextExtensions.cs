using System.Text;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

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

    public static bool IsCurrent(this IApplicationInsightsContext item)
    {
        return item?.TenantId == "F93C53EB-1C33-4EAC-B468-585F45ED9A5F";
    }

    public static IApplicationInsightsContext Current => new Ctx
    {
        TenantId = "F93C53EB-1C33-4EAC-B468-585F45ED9A5F",
        WorkspaceId = null,
        ClientId = null,
        ClientSecret = null,
    };

    internal record Ctx : IApplicationInsightsContext
    {
        public required string TenantId { get; init; }
        public required string WorkspaceId { get; init; }
        public required string ClientId { get; init; }
        public required string ClientSecret { get; init; }

        public static Ctx Current;
    }
}