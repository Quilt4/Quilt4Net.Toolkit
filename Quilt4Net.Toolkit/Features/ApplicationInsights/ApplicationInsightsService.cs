using System.Text.Json;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;

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
        var query = @$"AppExceptions | where SeverityLevel >= {(int)minSeverityLevel}
| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
| extend ProblemIdentifier = ProblemId
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
| project AppName, SeverityLevel, TimeGenerated, ProblemIdentifier, OuterMessage, Environment
| summarize IssueCount=count(), Message=take_any(OuterMessage), LastSeen=max(TimeGenerated), Environment=take_any(Environment) by AppName, SeverityLevel, ProblemIdentifier";
        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
                     {
                         SummaryIdentifier = BuildSummaryIdentifier(x, LogType.Exception),
                         Type = LogType.Exception,
                         Application = x["AppName"].ToString(),
                         SeverityLevel = (SeverityLevel)Convert.ToInt32(x["SeverityLevel"]),
                         IssueCount = Convert.ToInt32(x["IssueCount"]),
                         Message = x["Message"].ToString(),
                         LastSeen = DateTime.TryParse(x["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
                         Environment = x["Environment"]?.ToString()
            }))
            {
                yield return logErrorData;
            }
        }

        //NOTE: Pull data from traces
        query = @$"AppTraces | where SeverityLevel >= {(int)minSeverityLevel}
| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
| extend OriginalMessage = tostring(Properties['OriginalFormat'])
| extend MessageId = tostring(hash(Message))
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
| project AppName, SeverityLevel, OriginalMessage, MessageId, TimeGenerated, Environment
| summarize IssueCount=count(), MessageId=take_any(MessageId), LastSeen=max(TimeGenerated), Environment=take_any(Environment) by AppName, SeverityLevel, OriginalMessage";
        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            var p = table.Rows.FirstOrDefault()?["LastSeen"];

            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
            {
                SummaryIdentifier = BuildSummaryIdentifier(x, LogType.Trace),
                Type = LogType.Trace,
                Application = x["AppName"].ToString(),
                SeverityLevel = (SeverityLevel)Convert.ToInt32(x["SeverityLevel"]),
                IssueCount = Convert.ToInt32(x["IssueCount"]),
                Message = Convert.ToString(x["OriginalMessage"]),
                LastSeen = DateTime.TryParse(x["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
                Environment = x["Environment"]?.ToString()
            }))
            {
                yield return logErrorData;
            }
        }

        //NOTE: Pull data from requests
        query = @$"AppRequests
| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
| extend NameKey = tostring(hash(Url))
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Environment = tostring(Properties['AspNetCoreEnvironment'])
| project AppName, Name, NameKey, TimeGenerated, Environment
| summarize IssueCount=count(), NameKey=take_any(NameKey), LastSeen=max(TimeGenerated), Environment=take_any(Environment) by AppName, Name";
        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
                     {
                         SummaryIdentifier = BuildSummaryIdentifier(x, LogType.Request),
                         Type = LogType.Request,
                         Application = x["AppName"].ToString(),
                         SeverityLevel = SeverityLevel.Information,
                         IssueCount = Convert.ToInt32(x["IssueCount"]),
                         Message = Convert.ToString(x["Name"]),
                         LastSeen = DateTime.TryParse(x["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
                         Environment = x["Environment"]?.ToString()
            }))
            {
                yield return logErrorData;
            }
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
                    Identifier = x["ProblemIdentifier"]?.ToString(),
                    Application = x["AppName"]?.ToString()
                }));
            case LogType.Trace:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["MessageId"].ToString(),
                    Application = x["AppName"].ToString()
                }));
            case LogType.Request:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["Name"].ToString(),
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

    public async Task<LogDetails> GetDetails(string environment, string summaryIdentifier)
    {
        var summaryDataIdentifier = JsonSerializer.Deserialize<SummaryDataIdentifier>(Base64UrlHelper.DecodeFromBase64Url(summaryIdentifier));

        var client = GetClient();

        string detailQuery;
        switch (summaryDataIdentifier.Type)
        {
            case LogType.Exception:
                detailQuery = @$"AppExceptions
| where ProblemId == '{summaryDataIdentifier.Identifier}'
| where Properties['AspNetCoreEnvironment'] == '{environment}'
| where coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName) == '{summaryDataIdentifier.Application}'
| order by TimeGenerated desc | take 1";
                break;
            case LogType.Trace:
                detailQuery = @$"AppTraces
| where tostring(hash(Message)) == '{summaryDataIdentifier.Identifier}'
| where Properties['AspNetCoreEnvironment'] == '{environment}'
| where coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName) == '{summaryDataIdentifier.Application}'
| order by TimeGenerated desc | take 1";
                break;
            case LogType.Request:
                detailQuery = @$"AppRequests
| where Name == '{summaryDataIdentifier.Identifier}'
| order by TimeGenerated desc | take 1";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return await BuildItem(client, detailQuery, TimeSpan.FromDays(7));
    }

    private async Task<LogDetails> BuildItem(LogsQueryClient client, string detailQuery, TimeSpan timeSpan)
    {
        var detailedResponse = await client.QueryWorkspaceAsync(_options.WorkspaceId, detailQuery, new QueryTimeRange(timeSpan, DateTimeOffset.Now));

        var l = detailedResponse.Value.Table.Rows.Count;
        var rows = detailedResponse.Value.Table.Rows;
        var row = rows.FirstOrDefault();
        if (row == null) return default;

        var js = ConvertRowToJson(row, detailedResponse.Value.Table.Columns);
        var result = JsonSerializer.Deserialize<LogDetails>(js, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Raw = js;

        return result;
    }

    public async IAsyncEnumerable<LogMeasurement> GetMeasurements(string environment, TimeSpan timeSpan)
    {
        var client = GetClient();

        var query = @$"AppTraces | where isnotempty(Properties['Elapsed'])
| where Properties['AspNetCoreEnvironment'] == '{environment}'
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Elapsed = Properties['Elapsed']
| extend Action = Properties['Action']
| extend Method = Properties['Method']
| extend Path = Properties['Path']
| extend DetailsMethod = Properties['Details']
| extend CategoryName = Properties['CategoryName']
| order by TimeGenerated desc";
        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logMeasurement in table.Rows.Select(x =>
                     {
                         var details = GetDetails(x);

                         var action = x["Action"]?.ToString();
                         var method = x["Method"]?.ToString();
                         var path = x["Path"]?.ToString();

                         return new LogMeasurement
                         {
                             Application = x["AppName"].ToString(),
                             TimeGenerated = DateTime.TryParse(x["TimeGenerated"].ToString(), out var tg) ? tg : DateTime.MinValue,
                             Elapsed = TimeSpan.TryParse(x["Elapsed"].ToString(), out var d) ? d : TimeSpan.Zero,
                             CategoryName = x["CategoryName"].ToString(),
                             Method = details.Method,
                             Action = action ?? $"{method}{path}",
                         };
                     }))
            {
                yield return logMeasurement;
            }
        }

        query = @$"AppRequests
| where isnotempty(Properties['Elapsed'])
| where Properties['AspNetCoreEnvironment'] == '{environment}'
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| extend Elapsed = Properties['Elapsed']
| extend Action = Properties['Action']
| extend Method = Properties['Method']
| extend Path = Properties['Path']
| extend DetailsMethod = Properties['Details']
| extend CategoryName = Properties['CategoryName']
| order by TimeGenerated desc";
        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logMeasurement in table.Rows.Select(x =>
                     {
                         var details = GetDetails(x);

                         var method = x["Method"]?.ToString();
                         var path = x["Path"]?.ToString();
                         var action = x["Action"]?.ToString() ?? x["Name"]?.ToString() ?? $"{method}{path}";

                         var application = x["AppName"]?.ToString();
                         var readOnlySpan = x["TimeGenerated"]?.ToString();
                         var categoryName = x["CategoryName"]?.ToString();
                         var onlySpan = x["Elapsed"]?.ToString();

                         return new LogMeasurement
                         {
                             Application = application,
                             TimeGenerated = DateTime.TryParse(readOnlySpan, out var tg) ? tg : DateTime.MinValue,
                             Elapsed = TimeSpan.TryParse(onlySpan, out var d) ? d : TimeSpan.Zero,
                             CategoryName = categoryName,
                             Method = details.Method,
                             Action = action,
                         };
                     }))
            {
                yield return logMeasurement;
            }
        }
    }

    public async IAsyncEnumerable<LogItem> SearchAsync(string environment, string correlationId, TimeSpan timeSpan)
    {
        var client = GetClient();

        var query = @$"AppExceptions
| where Properties['AspNetCoreEnvironment'] == '{environment}'
| where Properties['CorrelationId'] == '{correlationId}'
| extend ProblemIdentifier = ProblemId
| extend Message = coalesce(Message,OuterMessage)
| extend AppName = coalesce(Properties['Source'], Properties['ApplicationName'], AppRoleName)
| order by TimeGenerated desc";
        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logErrorData in table.Rows.Select(x => new LogItem
                     {
                         SummaryIdentifier = BuildSummaryIdentifier(x, LogType.Exception),
                         Type = LogType.Exception,
                         Application = x["AppName"]?.ToString(),
                         SeverityLevel = (SeverityLevel)Convert.ToInt32(x["SeverityLevel"]),
                         Message = x["Message"]?.ToString()
                     }))
            {
                yield return logErrorData;
            }
        }
    }

    private static (string Monitor, string Method) GetDetails(LogsTableRow x)
    {
        var dm = x["DetailsMethod"]?.ToString();
        if (dm != null)
        {
            using JsonDocument jsonDoc = JsonDocument.Parse(dm);
            var method = jsonDoc.RootElement.GetProperty("Method").GetString();
            var monitor = jsonDoc.RootElement.GetProperty("Monitor").GetString();
            return (monitor, method);
        }

        return (null, null);
    }

    private static string ConvertRowToJson(LogsTableRow row, IReadOnlyList<LogsTableColumn> columns)
    {
        // Create a dictionary to hold the column names and values
        var rowDictionary = new Dictionary<string, object>();

        // Iterate over each column in the row and add it to the dictionary
        for (var i = 0; i < columns.Count; i++)
        {
            var columnName = columns[i].Name;
            var columnValue = row[i]; // Get the value at the current index

            if (columns[i].Type == LogsColumnType.Dynamic && columnValue != null)
            {
                var parsedJson = JsonSerializer.Deserialize<object>($"{columnValue}");
                rowDictionary[columnName] = parsedJson;
            }
            else
            {
                // Add the column name and value to the dictionary
                rowDictionary[columnName] = columnValue;
            }
        }

        // Serialize the dictionary to JSON
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true // Makes JSON output readable
        };
        return JsonSerializer.Serialize(rowDictionary, jsonOptions);
    }
}