using System.Text.Json;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal class ApplicationInsightsClient : IApplicationInsightsClient
{
    private readonly Quilt4NetApplicationInsightsOptions _options;

    public ApplicationInsightsClient(Quilt4NetApplicationInsightsOptions options)
    {
        _options = options;
    }

    public async IAsyncEnumerable<SummaryData> GetSummaryAsync(string environment)
    {
        var client = GetClient();

        //NOTE: Pull data from exceptions
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
        query = $"AppTraces | where SeverityLevel >= 0 | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend OriginalMessage = tostring(Properties['OriginalFormat']) | extend MessageId = coalesce(OperationId,tostring(hash(Message))) | summarize IssueCount=count(), Message=take_any(OriginalMessage), MessageId=take_any(MessageId) by AppRoleName, SeverityLevel";
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
                detailQuery = $"AppExceptions | where ProblemId == '{summaryDataIdentifier.Identifier}' | where Properties['AspNetCoreEnvironment'] == '{environment}' | where AppRoleName == '{summaryDataIdentifier.Application}' | order by TimeGenerated desc | take 1";
                break;
            case LogType.Trace:
                detailQuery = $"AppTraces | where OperationId == '{summaryDataIdentifier.Identifier}' or tostring(hash(Message)) == '{summaryDataIdentifier.Identifier}' | where Properties['AspNetCoreEnvironment'] == '{environment}' | where AppRoleName == '{summaryDataIdentifier.Application}' | order by TimeGenerated desc | take 1";
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
        var result = JsonSerializer.Deserialize<LogDetails>(js, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Raw = js;

        return result;
    }

    public async IAsyncEnumerable<LogMeasurement> GetMeasurements(string environment)
    {
        var client = GetClient();

        var query = $"AppTraces | where isnotempty(Properties['Elapsed']) | where Properties['AspNetCoreEnvironment'] == '{environment}' | extend Elapsed = Properties['Elapsed'] | extend Action = Properties['Action'] | extend Method = Properties['Method'] | extend Path = Properties['Path'] | extend DetailsMethod = Properties['Details'] | extend CategoryName = Properties['CategoryName'] | order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(_options.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromDays(7), DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            //var cols = string.Join("###", table.Columns.Select(x => x.Name).ToArray());
            //var row1 = string.Join("###", table.Rows.First().ToArray());
            ////var aa = table.Rows.ToArray();
            foreach (var logMeasurement in table.Rows.Select(x =>
                     {
                         var details = GetDetails(x);

                         var action = x["Action"]?.ToString();
                         var method = x["Method"]?.ToString();
                         var path = x["Path"]?.ToString();

                         return new LogMeasurement
                         {
                             Application = x["AppRoleName"].ToString(), //TODO: Add fallback
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

    //TODO: Create special http-request feature....
    //"Request": "{\"Method\":\"GET\",\"Path\":\"/ApplicationInsights/measurements\",\"Headers\":{\"Accept\":\"*/*\",\"Host\":\"localhost:7119\",\"User-Agent\":\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36\",\"Accept-Encoding\":\"gzip, deflate, br, zstd\",\"Accept-Language\":\"sv-SE,sv;q=0.9,en-SE;q=0.8,en-US;q=0.7,en;q=0.6\",\"Referer\":\"https://localhost:7119/swagger/index.html\",\"sec-ch-ua-platform\":\"\\u0022Windows\\u0022\",\"sec-ch-ua\":\"\\u0022Google Chrome\\u0022;v=\\u0022131\\u0022, \\u0022Chromium\\u0022;v=\\u0022131\\u0022, \\u0022Not_A Brand\\u0022;v=\\u002224\\u0022\",\"sec-ch-ua-mobile\":\"?0\",\"sec-fetch-site\":\"same-origin\",\"sec-fetch-mode\":\"cors\",\"sec-fetch-dest\":\"empty\",\"DNT\":\"1\",\"sec-gpc\":\"1\",\"priority\":\"u=1, i\"},\"Query\":{\"environment\":\"Development\"},\"Body\":\"\",\"ClientIp\":\"::1\"}",
    //"Response": "{\"StatusCode\":200,\"Headers\":{\"Content-Type\":\"application/json; charset=utf-8\",\"Request-Context\":\"appId=cid-v1:7c2b7502-9c27-4c02-8bde-28b3eaec2a65\"},\"Body\":\"[{\\u0022application\\u0022:\\u0022\\u0022},{\\u0022application\\u0022:\\u0022\\u0022},{\\u0022application\\u0022:\\u0022\\u0022},{\\u0022application\\u0022:\\u0022\\u0022},{\\u0022application\\u0022:\\u0022\\u0022},{\\u0022application\\u0022:\\u0022\\u0022}]\"}",

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