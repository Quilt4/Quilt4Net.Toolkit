using System.Text.Json.Serialization;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

public record LogDetails
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    public required DateTime TimeGenerated { get; init; }
    public required string Message { get; init; }
    public required string ProblemId { get; init; }
    public string AppName { get; init; }
    public string Environment { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SeverityLevel SeverityLevel { get; init; }

    public required string Raw { get; init; }

    //public Dictionary<string, string> Properties { get; init; }
    //public object Measurements { get; init; }
    //public string OperationName { get; init; }
    //public string OperationId { get; init; }
    //public string ParentId { get; init; }
    //public string SyntheticSource { get; init; }
    //public string SessionId { get; init; }
    //public string UserId { get; init; }
    //public string UserAuthenticatedId { get; init; }
    //public string UserAccountId { get; init; }
    //public string AppVersion { get; init; }
    //public string AppRoleName { get; init; }
    //public string AppRoleInstance { get; init; }
    //public string ClientType { get; init; }
    //public string ClientModel { get; init; }
    //public string ClientOS { get; init; }
    //public string ClientIP { get; init; }
    //public string ClientCity { get; init; }
    //public string ClientStateOrProvince { get; init; }
    //public string ClientCountryOrRegion { get; init; }
    //public string ClientBrowser { get; init; }
    //public string ResourceGUID { get; init; }
    //public string IKey { get; init; }
    //public string SDKVersion { get; init; }
    //public int ItemCount { get; init; }
    //public string ReferencedItemId { get; init; }
    //public string ReferencedType { get; init; }
    //public string SourceSystem { get; init; }
    //public string Type { get; init; }
    //public string _ResourceId { get; init; }
    //public string ProblemId { get; init; }
    //public string HandledAt { get; init; }
    //public string ExceptionType { get; init; }
    //public string Assembly { get; init; }
    //public string Method { get; init; }
    //public string OuterType { get; init; }
    //public string OuterMessage { get; init; }
    //public string OuterAssembly { get; init; }
    //public string OuterMethod { get; init; }
    //public string InnermostType { get; init; }
    //public string InnermostMessage { get; init; }
    //public string InnermostAssembly { get; init; }
    //public string InnermostMethod { get; init; }
    //public Detail[] Details { get; init; }
}