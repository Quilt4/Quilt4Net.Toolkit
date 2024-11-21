namespace Quilt4Net.Toolkit;

public class LogDetails
{
    public string TenantId { get; set; }
    public DateTime TimeGenerated { get; set; }
    public string Message { get; set; }
    public int SeverityLevel { get; set; }
    //public Properties Properties { get; set; }
    public object Measurements { get; set; }
    public string OperationName { get; set; }
    public string OperationId { get; set; }
    public string ParentId { get; set; }
    public string SyntheticSource { get; set; }
    public string SessionId { get; set; }
    public string UserId { get; set; }
    public string UserAuthenticatedId { get; set; }
    public string UserAccountId { get; set; }
    public string AppVersion { get; set; }
    public string AppRoleName { get; set; }
    public string AppRoleInstance { get; set; }
    public string ClientType { get; set; }
    public string ClientModel { get; set; }
    public string ClientOS { get; set; }
    public string ClientIP { get; set; }
    public string ClientCity { get; set; }
    public string ClientStateOrProvince { get; set; }
    public string ClientCountryOrRegion { get; set; }
    public string ClientBrowser { get; set; }
    public string ResourceGUID { get; set; }
    public string IKey { get; set; }
    public string SDKVersion { get; set; }
    public int ItemCount { get; set; }
    public string ReferencedItemId { get; set; }
    public string ReferencedType { get; set; }
    public string SourceSystem { get; set; }
    public string Type { get; set; }
    public string _ResourceId { get; set; }
    public string ProblemId { get; set; }
    public string HandledAt { get; set; }
    public string ExceptionType { get; set; }
    public string Assembly { get; set; }
    public string Method { get; set; }
    public string OuterType { get; set; }
    public string OuterMessage { get; set; }
    public string OuterAssembly { get; set; }
    public string OuterMethod { get; set; }
    public string InnermostType { get; set; }
    public string InnermostMessage { get; set; }
    public string InnermostAssembly { get; set; }
    public string InnermostMethod { get; set; }
    //public Detail[] Details { get; set; }
}

//public class Properties
//{
//    public string AspNetCoreEnvironment { get; set; }
//    public string _MSProcessedByMetricExtractors { get; set; }
//    public string FormattedMessage { get; set; }
//    public string OriginalFormat { get; set; }
//    public string CategoryName { get; set; }
//    public string ConnectionId { get; set; }
//    public string RequestId { get; set; }
//    public string RequestPath { get; set; }
//    public string ParentId { get; set; }
//    public string SpanId { get; set; }
//    public string TraceId { get; set; }
//    public string ActionName { get; set; }
//    public string ActionId { get; set; }
//}

//public class Detail
//{
//    public string severityLevel { get; set; }
//    public string outerId { get; set; }
//    public string message { get; set; }
//    public string type { get; set; }
//    public string id { get; set; }
//    public Parsedstack[] parsedStack { get; set; }
//}

//public class Parsedstack
//{
//    public string assembly { get; set; }
//    public string method { get; set; }
//    public int level { get; set; }
//    public int line { get; set; }
//    public string fileName { get; set; }
//}
