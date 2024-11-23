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
        //var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | summarize IssueCount=count() by AppRoleName, SeverityLevel, ProblemId";
        //var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend ProblemIdentifier = coalesce(ProblemId, Message, 'Unknown') | summarize IssueCount=count() by AppRoleName, SeverityLevel, ProblemIdentifier";
        //var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend ProblemIdentifier = coalesce(ProblemId, Message) | summarize IssueCount=count(), Message = arg_min(timestamp, Message), UniqueId = arg_min(timestamp, Id) by AppRoleName, SeverityLevel, ProblemIdentifier";
        var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend ProblemIdentifier = coalesce(ProblemId, Message) | summarize IssueCount=count(), Message = arg_min(timestamp, Message) by AppRoleName, SeverityLevel, ProblemIdentifier";

        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
                     {
                         AppRoleName = x["AppRoleName"].ToString(),
                         SeverityLevel = (SeverityLevel)Convert.ToInt32(x["SeverityLevel"]),
                         ProblemId = Convert.ToString(x["ProblemIdentifier"]),
                         IssueCount = Convert.ToInt32(x["IssueCount"]),
                         Message = x["Message"].ToString(),
                         UniqueId = x["UniqueId"].ToString()
                     }))
            {
                yield return logErrorData;
            }
        }
    }

    private LogsQueryClient GetClient()
    {
        var clientSecretCredential = new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
        var client = new LogsQueryClient(clientSecretCredential);
        return client;
    }

    public async Task<LogDetails> GetDetails(string environment, string appRoleName, string problemId)
    {
        var client = GetClient();
        var detailQuery = $"AppTraces | union AppExceptions | where ProblemId == '{problemId}' or Message == '{problemId}' | where Properties['AspNetCoreEnvironment'] == '{environment}' | where AppRoleName == '{appRoleName}' | order by TimeGenerated desc | take 1";

        //NOTE: This is to make a detailed query about one issue
        var detailedResponse = await client.QueryWorkspaceAsync(_options.WorkspaceId, detailQuery, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));
        var rows = detailedResponse.Value.Table.Rows;
        var row = rows.FirstOrDefault();
        if (row == null) return default;
        var js = ConvertRowToJson(row, detailedResponse.Value.Table.Columns);
        var result = JsonSerializer.Deserialize<LogDetails>(js, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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