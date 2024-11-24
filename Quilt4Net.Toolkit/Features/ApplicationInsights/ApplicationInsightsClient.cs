using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal class ApplicationInsightsClient : IApplicationInsightsClient
{
    private readonly Quilt4NetOptions _options;

    public ApplicationInsightsClient(Quilt4NetOptions options)
    {
        _options = options;
    }

    public async IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment)
    {
        var client = GetClient();

        //NOTE: Pull data from exceptions
        //var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | summarize IssueCount=count() by AppRoleName, SeverityLevel, ProblemId";
        //var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend ProblemIdentifier = coalesce(ProblemId, Message, 'Unknown') | summarize IssueCount=count() by AppRoleName, SeverityLevel, ProblemIdentifier";
        //var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend ProblemIdentifier = coalesce(ProblemId, Message) | summarize IssueCount=count(), Message = arg_min(timestamp, Message), UniqueId = arg_min(timestamp, Id) by AppRoleName, SeverityLevel, ProblemIdentifier";
        var query = $"AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend ProblemIdentifier = ProblemId | summarize IssueCount=count(), Message = take_any(OuterMessage) by AppRoleName, SeverityLevel, ProblemIdentifier";

        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
            {
                SummaryIdentifier = BuildSummaryIdentifier(x, LogType.Exception),
                Type = LogType.Exception,
                Application = x["AppRoleName"].ToString(),
                SeverityLevel = (SeverityLevel)Convert.ToInt32(x["SeverityLevel"]),
                IssueCount = Convert.ToInt32(x["IssueCount"]),
                Message = x["Message"].ToString()
            }))
            {
                yield return logErrorData;
            }
        }

        //NOTE: Pull data from traces
        //var query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | summarize IssueCount=count() by AppRoleName, SeverityLevel"; //WORKS
        //var query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend Message = tostring(Properties['OriginalFormat']) | summarize IssueCount=count() | MessageId = take_any(operation_Id) by AppRoleName, SeverityLevel, Message";
        //var query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend Message = tostring(Properties['OriginalFormat']) | summarize IssueCount=count() | MessageId = arg_min(timestamp, Message) by AppRoleName, SeverityLevel, Message";
        //var query = $"AppTraces | extend msg = tostring(Properties['OriginalFormat']) | summarize IssueCount = count(), FirstMessage = take_any(msg), severityLevel by AppRoleName, SeverityLevel";
        //var query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend OriginalMessage = tostring(Properties['OriginalFormat']) | summarize IssueCount=count() by AppRoleName, SeverityLevel"; //WORKS
        //var query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend OriginalMessage = tostring(Properties['OriginalFormat']) | summarize IssueCount=count(), FirstMessag=take_any(OriginalMessage) by AppRoleName, SeverityLevel"; //WORKS
        //var query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend OriginalMessage = tostring(Properties['OriginalFormat']) | summarize IssueCount=count(), Message=take_any(OriginalMessage) by AppRoleName, SeverityLevel"; //WORKS
        query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend OriginalMessage = tostring(Properties['OriginalFormat'])| extend MessageId = coalesce(OperationId,tostring(hash(Message))) | summarize IssueCount=count(), Message=take_any(OriginalMessage), MessageId=take_any(MessageId) by AppRoleName, SeverityLevel";
        //var query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | summarize IssueCount=count(), FirstMessage=take_any(msg) by AppRoleName, SeverityLevel";
        response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
            {
                SummaryIdentifier = BuildSummaryIdentifier(x, LogType.Trace),
                Type = LogType.Trace,
                Application = x["AppRoleName"].ToString(),
                SeverityLevel = (SeverityLevel)Convert.ToInt32(x["SeverityLevel"]),
                IssueCount = Convert.ToInt32(x["IssueCount"]),
                Message = Convert.ToString(x["Message"]),
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
                    Identifier = x["ProblemIdentifier"].ToString(),
                    Application = x["AppRoleName"].ToString()
                }));
            case LogType.Trace:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["MessageId"].ToString(),
                    Application = x["AppRoleName"].ToString()
                }));
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private LogsQueryClient GetClient()
    {
        var clientSecretCredential = new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
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
                detailQuery = $"union AppExceptions | where ProblemId == '{summaryDataIdentifier.Identifier}' | where Properties['AspNetCoreEnvironment'] == '{environment}' | where AppRoleName == '{summaryDataIdentifier.Application}' | order by TimeGenerated desc | take 1";
                break;
            case LogType.Trace:
                detailQuery = $"union AppTraces | where OperationId == '{summaryDataIdentifier.Identifier}' or tostring(hash(Message)) == '{summaryDataIdentifier.Identifier}' | where Properties['AspNetCoreEnvironment'] == '{environment}' | where AppRoleName == '{summaryDataIdentifier.Application}' | order by TimeGenerated desc | take 1";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //var detailQuery = $"AppTraces | union AppExceptions | where ProblemId == '{problemId}' or Message == '{problemId}' | where Properties['AspNetCoreEnvironment'] == '{environment}' | where AppRoleName == '{appRoleName}' | order by TimeGenerated desc | take 1";
        var detailedResponse = await client.QueryWorkspaceAsync(_options.WorkspaceId, detailQuery, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));

        var rows = detailedResponse.Value.Table.Rows;
        var row = rows.FirstOrDefault();
        if (row == null) return default;

        var js = ConvertRowToJson(row, detailedResponse.Value.Table.Columns);
        var result = JsonSerializer.Deserialize<LogDetails>(js, new JsonSerializerOptions //TODO: Kan vara olika details beroende på om det är exception eller trace.
        {
            PropertyNameCaseInsensitive = true
        });

        result.Raw = js;

        return result;
    }

    static string ConvertRowToJson(LogsTableRow row, IReadOnlyList<LogsTableColumn> columns)
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

public class Base64UrlHelper
{
    public static string EncodeToBase64Url(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Convert the input string to bytes
        byte[] bytes = Encoding.UTF8.GetBytes(input);

        // Convert to Base64
        string base64 = Convert.ToBase64String(bytes);

        // Make it URL-safe by replacing special characters and removing padding
        string base64Url = base64
            .Replace("+", "-")  // Replace '+' with '-'
            .Replace("/", "_")  // Replace '/' with '_'
            .TrimEnd('=');      // Remove padding '='

        return base64Url;
    }

    public static string DecodeFromBase64Url(string base64Url)
    {
        if (string.IsNullOrEmpty(base64Url))
            return string.Empty;

        // Convert Base64 URL-safe to standard Base64 by restoring characters and padding
        string base64 = base64Url
            .Replace("-", "+")  // Replace '-' with '+'
            .Replace("_", "/"); // Replace '_' with '/'

        // Add padding if necessary
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        // Convert the Base64 string back to bytes
        byte[] bytes = Convert.FromBase64String(base64);

        // Convert bytes to string
        return Encoding.UTF8.GetString(bytes);
    }
}
