using System.Diagnostics;
using System.Text.Json;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;

namespace Quilt4Net.Toolkit;

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
        var query = $"AppTraces | union AppExceptions | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | summarize issueCount=count() by AppRoleName, SeverityLevel, ProblemId";

        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var logErrorData in table.Rows.Select(x => new SummaryData
                     {
                         AppRoleName = x["AppRoleName"].ToString(),
                         SeverityLevel = (SeverityLevel)Convert.ToInt32(x["SeverityLevel"]),
                         ProblemId = Convert.ToString(x["ProblemId"]),
                         IssueCount = Convert.ToInt32(x["issueCount"])
                     }))
            {
                yield return logErrorData;
            }
        }

        //foreach (var group in data
        //             //.Where(x => x.AppRoleName == "app-eplicta-newaggregator-prod")
        //             //.Where(x => x.AppRoleName == "app-eplicta-agent-test")
        //             .GroupBy(x => new { RoleName = x.AppRoleName, x.SeverityLevel, x.ProblemId })
        //             .OrderByDescending(x => x.First().SeverityLevel)
        //             .ThenByDescending(x => x.Sum(y => y.IssueCount))
        //        )
        //{
        //    Console.WriteLine($"{group.Key.RoleName} (Severity: {group.Key.SeverityLevel}, {group.Sum(x => x.IssueCount)} issues) for {group.First().ProblemId}");
        //}
        //Console.WriteLine($"Total issue count: {data.Sum(x => x.IssueCount)}");
    }

    private LogsQueryClient GetClient()
    {
        var clientSecretCredential = new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret);
        var client = new LogsQueryClient(clientSecretCredential);
        return client;
    }

    public async Task<LogDetails> GetDetails(string environment, string appRoleName, string problemId)
    {
        //System.InvalidCastException at Eplicta.Fido.Features.GetPublication.PublicationAccessNotifyAggregatorBehavior.UpdateCache
        //app-eplicta-fido-prod

        var client = GetClient();
        var detailQuery = $@"AppTraces | union AppExceptions | where ProblemId == '{problemId}' | where Properties['AspNetCoreEnvironment'] == '{environment}' | where AppRoleName == '{appRoleName}' | order by TimeGenerated desc | take 1";

        //NOTE: This is to make a detailed query about one issue
        var detailedResponse = await client.QueryWorkspaceAsync(_options.WorkspaceId, detailQuery, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));
        var columns = detailedResponse.Value.Table.Columns;
        var rows = detailedResponse.Value.Table.Rows;
        var row = rows.First();
        var js = ConvertRowToJson(row, detailedResponse.Value.Table.Columns);
        //Console.WriteLine(js);

        Debugger.Break();
        throw new NotImplementedException();
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