using System.Diagnostics;
using System.Text.Json;
using Azure;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Quilt4Net.Toolkit.Framework;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal class ApplicationInsightsService : IApplicationInsightsService
{
    private readonly Quilt4NetApplicationInsightsOptions _options;

    public ApplicationInsightsService(Quilt4NetApplicationInsightsOptions options)
    {
        _options = options;
    }

    public async IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment, TimeSpan timeSpan, SeverityLevel minSeverityLevel)
    {
        var client = GetClient();

        //NOTE: Pull data from exceptions
        var query = @$"AppExceptions
| where SeverityLevel >= {(int)minSeverityLevel}
| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
| extend ProblemId = tostring(hash(strcat(ProblemId, Properties['OriginalFormat'])))
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
| extend Message = OuterMessage
| project AppName, Environment, SeverityLevel, TimeGenerated, ProblemId, Message
| summarize
    AppName = take_any(AppName),
    Environment = take_any(Environment),
    SeverityLevel = take_any(SeverityLevel),
    LastSeen = max(TimeGenerated),
    Message = take_any(Message),
    IssueCount = count()
    by ProblemId";
        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var row in table.Rows)
            {
                yield return new SummaryData
                {
                    SummaryId = BuildSummaryIdentifier(row, LogType.Exception),
                    Application = row["AppName"].ToString(),
                    Environment = row["Environment"]?.ToString(),
                    Message = row["Message"].ToString(),
                    SeverityLevel = (SeverityLevel)Convert.ToInt32(row["SeverityLevel"]),
                    LastSeen = DateTime.TryParse(row["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
                    IssueCount = Convert.ToInt32(row["IssueCount"]),
                    Type = LogType.Exception
                };
            }
        }

        //NOTE: Pull data from traces
        query = @$"AppTraces
| where SeverityLevel >= 0
| where Properties['AspNetCoreEnvironment'] == 'Production' or isempty(Properties['AspNetCoreEnvironment'])
| extend ProblemId = tostring(hash(tostring(Properties['OriginalFormat'])))
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
| project AppName, Environment, SeverityLevel, TimeGenerated, ProblemId, Message
| summarize
    AppName = take_any(AppName),
    Environment = take_any(Environment),
    SeverityLevel = take_any(SeverityLevel),
    LastSeen = max(TimeGenerated),
    Message = take_any(Message),
    IssueCount = count()
    by ProblemId";
        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var row in table.Rows)
            {
                yield return new SummaryData
                {
                    SummaryId = BuildSummaryIdentifier(row, LogType.Trace),
                    Application = row["AppName"].ToString(),
                    Environment = row["Environment"]?.ToString(),
                    Message = Convert.ToString(row["Message"]),
                    SeverityLevel = (SeverityLevel)Convert.ToInt32(row["SeverityLevel"]),
                    LastSeen = DateTime.TryParse(row["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
                    IssueCount = Convert.ToInt32(row["IssueCount"]),
                    Type = LogType.Trace
                };
            }
        }

        //        //NOTE: Pull data from requests
        //        query = @$"AppRequests
        //| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
        //| extend NameKey = tostring(hash(Url))
        //| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
        //| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
        //| project AppName, Name, NameKey, TimeGenerated, Environment
        //| summarize IssueCount=count(), NameKey=take_any(NameKey), LastSeen=max(TimeGenerated), Environment=take_any(Environment) by AppName, Name";
        //        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        //        foreach (var table in response.Value.AllTables)
        //        {
        //            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
        //                     {
        //                         SummaryIdentifier = BuildSummaryIdentifier(x, LogType.Request),
        //                         Type = LogType.Request,
        //                         Application = x["AppName"].ToString(),
        //                         SeverityLevel = SeverityLevel.Information,
        //                         IssueCount = Convert.ToInt32(x["IssueCount"]),
        //                         Message = Convert.ToString(x["Name"]),
        //                         LastSeen = DateTime.TryParse(x["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
        //                         Environment = x["Environment"]?.ToString()
        //            }))
        //            {
        //                yield return logErrorData;
        //            }
        //        }
    }

    private static string BuildItemIdentifier(LogsTableRow x, LogType type)
    {
        switch (type)
        {
            case LogType.Exception:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new ItemIdentifier
                {
                    Type = type,
                    Identifier = x["Id"]?.ToString()
                }));
            case LogType.Trace:
                //return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new ItemIdentifier
                //{
                //    Type = type,
                //    Identifier = x["MessageId"].ToString(),
                //    Application = x["AppName"].ToString()
                //}));
                throw new NotImplementedException();
            case LogType.Request:
                //return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new ItemIdentifier
                //{
                //    Type = type,
                //    Identifier = x["Name"].ToString(),
                //    Application = x["AppName"].ToString()
                //}));
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static string BuildSummaryIdentifier(LogsTableRow x, LogType type)
    {
        switch (type)
        {
            case LogType.Exception:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["ProblemId"]?.ToString(),
                    Application = x["AppName"]?.ToString()
                }));
            case LogType.Trace:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["ProblemId"].ToString(),
                    Application = x["AppName"].ToString()
                }));
            case LogType.Request:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["ProblemId"].ToString(),
                    Application = x["AppName"].ToString()
                }));
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private LogsQueryClient GetClient()
    {
        var optionsClientSecret = _options.ClientSecret;
        if (string.IsNullOrEmpty(optionsClientSecret)) throw new InvalidOperationException($"No {nameof(Quilt4NetApplicationInsightsOptions.ClientSecret)} has been configured.");
        var clientSecretCredential = new ClientSecretCredential(_options.TenantId, _options.ClientId, optionsClientSecret);
        var client = new LogsQueryClient(clientSecretCredential);
        return client;
    }

    public async IAsyncEnumerable<LogDetails> GetDetails(string environment, string summaryId, TimeSpan timeSpan)
    {
        var summaryIdentifier = JsonSerializer.Deserialize<SummaryDataIdentifier>(Base64UrlHelper.DecodeFromBase64Url(summaryId));

        var client = GetClient();

        string detailQuery;
        switch (summaryIdentifier.Type)
        {
            case LogType.Exception:
                detailQuery = @$"AppExceptions
| where tostring(hash(strcat(ProblemId, Properties['OriginalFormat']))) == '{summaryIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(coalesce(Message, OuterMessage)))))
| extend ProblemId = tostring(hash(strcat(ProblemId, Properties['OriginalFormat'])))
| order by TimeGenerated desc";
                break;
            case LogType.Trace:
                detailQuery = @$"AppTraces
| where tostring(hash(tostring(Properties['OriginalFormat']))) == '{summaryIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(Message))))
| extend ProblemId = tostring(hash(tostring(Properties['OriginalFormat'])))
| order by TimeGenerated desc";
                break;
            case LogType.Request:
//                detailQuery = @$"AppRequests
//| where Name == '{summaryDataIdentifier.Identifier}'
//| order by TimeGenerated desc | take 1";
                throw new NotImplementedException();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var detailedResponse = await client.QueryWorkspaceAsync(_options.WorkspaceId, detailQuery, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var row in detailedResponse.Value.Table.Rows.Where(x => x != null))
        {
            yield return GetRow(row, detailedResponse);
        }
    }

    public async Task<LogDetails> GetDetail(string environment, string id, TimeSpan timeSpan)
    {
        var itemIdentifier = JsonSerializer.Deserialize<ItemIdentifier>(Base64UrlHelper.DecodeFromBase64Url(id));

        var client = GetClient();

        string detailQuery;
        switch (itemIdentifier.Type)
        {
            case LogType.Exception:
                detailQuery = @$"AppExceptions
| where tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(coalesce(Message, OuterMessage))))) == '{itemIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(coalesce(Message, OuterMessage)))))
| extend ProblemId = tostring(hash(strcat(ProblemId, Properties['OriginalFormat'])))
| order by TimeGenerated desc";
                break;
            case LogType.Trace:
                detailQuery = @$"AppTraces
| where tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(Message)))) == '{itemIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(Message))))
| extend ProblemId = tostring(hash(tostring(Properties['OriginalFormat'])))
| order by TimeGenerated desc | take 1";
                break;
            case LogType.Request:
                //                detailQuery = @$"AppRequests
                //| where Name == '{summaryDataIdentifier.Identifier}'
                //| order by TimeGenerated desc | take 1";
                throw new NotImplementedException();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var detailedResponse = await client.QueryWorkspaceAsync(_options.WorkspaceId, detailQuery, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        var row = detailedResponse.Value.Table.Rows.FirstOrDefault();
        if (row == null) return null;
        return GetRow(row, detailedResponse);
    }

    private static LogDetails GetRow(LogsTableRow row, Response<LogsQueryResult> detailedResponse)
    {
        try
        {

            var propertiesData = (BinaryData)row["Properties"];
            var properties = JsonSerializer.Deserialize<JsonElement>(propertiesData);

            //string message = null;

            //if (row.Contains("Message"))
            //    message = row["Message"]?.ToString();

            //if (string.IsNullOrWhiteSpace(message) && row.Contains("OuterMessage"))
            //    message = row["OuterMessage"]?.ToString();

            //if (string.IsNullOrWhiteSpace(message) && row.Contains("InnermostMessage"))
            //    message = row["InnermostMessage"]?.ToString();

            //if (string.IsNullOrWhiteSpace(message))
            //{
            //}

            var rowDictionary = RowDictionary(row, detailedResponse.Value.Table.Columns);
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var raw = JsonSerializer.Serialize(rowDictionary, jsonOptions);

            var message = TryGetColumn(row, "Message")
                              ?? TryGetColumn(row, "OuterMessage")
                              ?? TryGetProperty(properties, "Message")
                              ?? TryGetProperty(properties, "OuterMessage")
                              ?? TryGetProperty(properties, "InnermostMessage")
                              ?? rowDictionary.GetValueOrDefault("Message")?.ToString().NullIfEmpty()
                              ?? rowDictionary.GetValueOrDefault("OuterMessage")?.ToString().NullIfEmpty()
                              ?? rowDictionary.GetValueOrDefault("InnermostMessage")?.ToString().NullIfEmpty();

            if (string.IsNullOrEmpty(message))
            {
            }

            return new LogDetails
            {
                Id = BuildItemIdentifier(row, LogType.Exception),
                TenantId = $"{row["TenantId"]}",
                TimeGenerated = DateTime.Parse($"{row["TimeGenerated"]}"),
                SeverityLevel = Enum.Parse<SeverityLevel>($"{row["SeverityLevel"]}"),
                Message = message,
                ProblemId = $"{row["ProblemId"]}",
                AppName = GetAppName(properties, row),
                Environment = GetEnvironment(properties),
                Raw = raw
            };
        }
        catch (Exception e)
        {
            Debugger.Break();
            Console.WriteLine(e);
            throw;
        }
    }

    private static string GetAppName(JsonElement properties, LogsTableRow row)
    {
        string appName;
        if (properties.TryGetProperty("Source", out var sourceProp))
            appName = sourceProp.GetString();
        else if (properties.TryGetProperty("ApplicationName", out var appNameProp))
            appName = appNameProp.GetString();
        else
            appName = row["AppRoleName"]?.ToString();
        return appName;
    }

    private static string GetEnvironment(JsonElement properties)
    {
        return properties.TryGetProperty("AspNetCoreEnvironment", out var envProp) ? envProp.GetString() : "";
    }

    public async IAsyncEnumerable<LogMeasurement> GetMeasurements(string environment, TimeSpan timeSpan)
    {
        //        var client = GetClient();

        //        var query = @$"AppTraces | where isnotempty(Properties['Elapsed'])
        //| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
        //| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
        //| extend Elapsed = Properties['Elapsed']
        //| extend Action = Properties['Action']
        //| extend Method = Properties['Method']
        //| extend Path = Properties['Path']
        //| extend DetailsMethod = Properties['Details']
        //| extend CategoryName = Properties['CategoryName']
        //| order by TimeGenerated desc";
        //        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        //        foreach (var table in response.Value.AllTables)
        //        {
        //            foreach (var logMeasurement in table.Rows.Select(x =>
        //                     {
        //                         var details = GetDetails(x);

        //                         var action = x["Action"]?.ToString();
        //                         var method = x["Method"]?.ToString();
        //                         var path = x["Path"]?.ToString();

        //                         return new LogMeasurement
        //                         {
        //                             Application = x["AppName"].ToString(),
        //                             TimeGenerated = DateTime.TryParse(x["TimeGenerated"].ToString(), out var tg) ? tg : DateTime.MinValue,
        //                             Elapsed = TimeSpan.TryParse(x["Elapsed"].ToString(), out var d) ? d : TimeSpan.Zero,
        //                             CategoryName = x["CategoryName"].ToString(),
        //                             Method = details.Method,
        //                             Action = action ?? $"{method}{path}",
        //                         };
        //                     }))
        //            {
        //                yield return logMeasurement;
        //            }
        //        }

        //        query = @$"AppRequests
        //| where isnotempty(Properties['Elapsed'])
        //| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
        //| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
        //| extend Elapsed = Properties['Elapsed']
        //| extend Action = Properties['Action']
        //| extend Method = Properties['Method']
        //| extend Path = Properties['Path']
        //| extend DetailsMethod = Properties['Details']
        //| extend CategoryName = Properties['CategoryName']
        //| order by TimeGenerated desc";
        //        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        //        foreach (var table in response.Value.AllTables)
        //        {
        //            foreach (var logMeasurement in table.Rows.Select(x =>
        //                     {
        //                         var details = GetDetails(x);

        //                         var method = x["Method"]?.ToString();
        //                         var path = x["Path"]?.ToString();
        //                         var action = x["Action"]?.ToString() ?? x["Name"]?.ToString() ?? $"{method}{path}";

        //                         var application = x["AppName"]?.ToString();
        //                         var readOnlySpan = x["TimeGenerated"]?.ToString();
        //                         var categoryName = x["CategoryName"]?.ToString();
        //                         var onlySpan = x["Elapsed"]?.ToString();

        //                         return new LogMeasurement
        //                         {
        //                             Application = application,
        //                             TimeGenerated = DateTime.TryParse(readOnlySpan, out var tg) ? tg : DateTime.MinValue,
        //                             Elapsed = TimeSpan.TryParse(onlySpan, out var d) ? d : TimeSpan.Zero,
        //                             CategoryName = categoryName,
        //                             Method = details.Method,
        //                             Action = action,
        //                         };
        //                     }))
        //            {
        //                yield return logMeasurement;
        //            }
        //        }
        throw new NotImplementedException();
        yield break;
    }

    public async IAsyncEnumerable<LogItem> SearchAsync(string environment, string text, TimeSpan timeSpan)
    {
        var client = GetClient();

        var query = @$"AppExceptions
| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
| where Properties['CorrelationId'] == '{text}' or coalesce(Message, OuterMessage) contains '{text}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(coalesce(Message, OuterMessage)))))
| extend Message = coalesce(Message, OuterMessage)
| extend ProblemId = tostring(hash(strcat(ProblemId, Properties['OriginalFormat'])))
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
| order by TimeGenerated desc";
        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = BuildItemIdentifier(row, LogType.Exception),
                    TimeGenerated = DateTime.Parse($"{row["TimeGenerated"]}"),
                    SummaryId = BuildSummaryIdentifier(row, LogType.Exception),
                    Message = row["Message"]?.ToString(),
                    CorrelationId = default, //TODO: Find this
                    Application = row["AppName"]?.ToString(),
                    Environment = row["Environment"]?.ToString(),
                    Type = LogType.Exception,
                    SeverityLevel = (SeverityLevel)Convert.ToInt32(row["SeverityLevel"])
                };
            }
        }

//TODO: Need to fix properties
        query = @$"AppTraces
| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
| where Properties['CorrelationId'] == '{text}' or Message contains '{text}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(Message))))
| extend ProblemId = tostring(hash(strcat(ProblemId, Properties['OriginalFormat'])))
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
| order by TimeGenerated desc";
        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = BuildItemIdentifier(row, LogType.Exception),
                    TimeGenerated = DateTime.Parse($"{row["TimeGenerated"]}"),
                    SummaryId = BuildSummaryIdentifier(row, LogType.Exception),
                    Message = row["Message"]?.ToString(),
                    CorrelationId = default, //TODO: Find this
                    Application = row["AppName"]?.ToString(),
                    Environment = row["Environment"]?.ToString(),
                    Type = LogType.Exception,
                    SeverityLevel = (SeverityLevel)Convert.ToInt32(row["SeverityLevel"])
                };
            }
        }

        //TODO: Search in Traces and Requests
    }

    private static Dictionary<string, object> RowDictionary(LogsTableRow row, IReadOnlyList<LogsTableColumn> columns)
    {
        var rowDictionary = new Dictionary<string, object>();
        for (var i = 0; i < columns.Count; i++)
        {
            var columnName = columns[i].Name;
            var columnValue = row[i];

            if (columns[i].Type == LogsColumnType.Dynamic && columnValue != null)
            {
                var parsedJson = JsonSerializer.Deserialize<object>($"{columnValue}");
                rowDictionary[columnName] = parsedJson;
            }
            else
            {
                rowDictionary[columnName] = columnValue;
            }
        }

        return rowDictionary;
    }

    private static string? TryGetColumn(LogsTableRow row, string key)
    {
        var result = row.Contains(key) ? row[key]?.ToString() : null;
        if (string.IsNullOrEmpty(result)) return null;
        return result;
    }

    private static string? TryGetProperty(JsonElement json, string key)
    {
        var result = json.TryGetProperty(key, out var val) ? val.GetString() : null;
        if (string.IsNullOrEmpty(result)) return null;
        return result;
    }
}