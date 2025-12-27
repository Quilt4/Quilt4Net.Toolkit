using Azure;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Framework;
using System.Diagnostics;
using System.Text.Json;

namespace Quilt4Net.Toolkit.Features.ApplicationInsights;

internal class ApplicationInsightsService : IApplicationInsightsService
{
    private readonly ApplicationInsightsOptions _options;

    public ApplicationInsightsService(IOptions<ApplicationInsightsOptions> options)
    {
        _options = options.Value;
    }

    public async IAsyncEnumerable<LogItem> SearchAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose)
    {
        var items = await SearchInternalAsync(context, environment, text, timeSpan, minSeverityLevel).ToArrayAsync();
        var duplicates = items.GroupBy(x => x.Id).Where(x => x.Count() > 1).ToArray();
        if (duplicates.Any())
        {
            //TODO: Fix error with Id, it should always be unique.
            Debugger.Break();
        }

        foreach (var item in items)
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<LogItem> SearchInternalAsync(IApplicationInsightsContext context, string environment, string text, TimeSpan timeSpan, SeverityLevel minSeverityLevel = SeverityLevel.Verbose)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        // =========================
        // AppExceptions
        // =========================
        var query = @$"
AppExceptions
| extend _p = todynamic(Properties)
| extend
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    ApplicationName = coalesce(
        tostring(_p[""ApplicationName""]),
        tostring(AppRoleName)
    ),
    Message = tostring(OuterMessage)
| where SeverityLevel >= {(int)minSeverityLevel}
| extend
    Id = tostring(hash(strcat(TimeGenerated, ""|"", OperationId, ""|"", ProblemId))),
    Fingerprint = tostring(ProblemId)
| project
    TimeGenerated,
    ApplicationName,
    Environment,
    Message,
    SeverityLevel,
    Id,
    Fingerprint,
    OperationId
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");

            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = BuildItemIdentifier(row, idIndex, LogSource.Exception),
                    Fingerprint = row[fingerprintIndex]?.ToString(),
                    TimeGenerated = GetDateTime(row, timeIndex),
                    Source = LogSource.Exception,
                    SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
                    Message = row[messageIndex]?.ToString(),
                    Environment = row[environmentIndex]?.ToString(),
                    Application = row[applicationIndex]?.ToString()
                };
            }
        }

        // =========================
        // AppTraces
        // =========================
        query = @$"
AppTraces
| extend _p = todynamic(Properties)
| extend
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    ApplicationName = coalesce(
        tostring(_p[""ApplicationName""]),
        tostring(AppRoleName)
    ),
    Message = tostring(Message),
    OriginalFormat = tostring(_p[""OriginalFormat""])
| where SeverityLevel >= {(int)minSeverityLevel}
| extend
    Id = tostring(hash(strcat(TimeGenerated, ""|"", OperationId, ""|"", Message))),
    Fingerprint = tostring(hash(OriginalFormat))
| project
    TimeGenerated,
    ApplicationName,
    Environment,
    Message,
    SeverityLevel,
    Id,
    Fingerprint,
    OperationId
| order by TimeGenerated desc";

        response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");

            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = BuildItemIdentifier(row, idIndex, LogSource.Trace),
                    Fingerprint = row[fingerprintIndex]?.ToString(),
                    TimeGenerated = GetDateTime(row, timeIndex),
                    Source = LogSource.Trace,
                    SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
                    Message = row[messageIndex]?.ToString(),
                    Environment = row[environmentIndex]?.ToString(),
                    Application = row[applicationIndex]?.ToString()
                };
            }
        }

        // =========================
        // AppRequests
        // =========================
        query = @$"
AppRequests
| extend _p = todynamic(Properties)
| extend
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    ApplicationName = coalesce(
        tostring(_p[""ApplicationName""]),
        tostring(AppRoleName)
    ),
    Message = tostring(Name)
| extend
    SeverityLevel = case(
        ResultCode startswith ""5"", 3,
        ResultCode startswith ""4"", 2,
        1
    )
| where SeverityLevel >= {(int)minSeverityLevel}
| extend
    Id = tostring(hash(strcat(TimeGenerated, ""|"", OperationId, ""|"", Message))),
    Fingerprint = tostring(hash(Message))
| project
    TimeGenerated,
    ApplicationName,
    Environment,
    Message,
    SeverityLevel,
    Id,
    Fingerprint,
    OperationId,
    ResultCode,
    Success
| order by TimeGenerated desc";

        response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var severityIndex = GetColumnIndex(table, "SeverityLevel");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");

            foreach (var row in table.Rows)
            {
                yield return new LogItem
                {
                    Id = BuildItemIdentifier(row, idIndex, LogSource.Request),
                    Fingerprint = row[fingerprintIndex]?.ToString(),
                    TimeGenerated = GetDateTime(row, timeIndex),
                    Source = LogSource.Request,
                    SeverityLevel = (SeverityLevel)GetInt(row, severityIndex),
                    Message = row[messageIndex]?.ToString(),
                    Environment = row[environmentIndex]?.ToString(),
                    Application = row[applicationIndex]?.ToString()
                };
            }
        }
    }

    public async IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        var items = await GetMeasureInternalAsync(context, environment, timeSpan).ToArrayAsync();
        var duplicates = items.GroupBy(x => x.Id).Where(x => x.Count() > 1).ToArray();
        if (duplicates.Any())
        {
            //TODO: Fix error with Id, it should always be unique.
            Debugger.Break();
        }

        foreach (var item in items)
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<MeasureData> GetMeasureInternalAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        var envFilter = environment ?? string.Empty;

        var query = @$"
AppTraces
| extend _p = todynamic(Properties)
| extend
    Method = tostring(parse_json(tostring(_p[""Details""]))[""Method""])
| where Method == ""Measure""
| extend
    Action = tostring(_p[""Action""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    ElapsedRaw = extract(@""in ([0-9:\.]+) ms"", 1, Message)
| extend
    Elapsed = totimespan(ElapsedRaw),
    Id = tostring(hash(strcat(TimeGenerated, ""|"", OperationId, ""|"", Message))),
    Fingerprint = tostring(hash(strcat(""Measure|"", Action)))
| project
    TimeGenerated,
    Id,
    Fingerprint,
    Message,
    Environment,
    ApplicationName,
    Action,
    Elapsed
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");
            var actionIndex = GetColumnIndex(table, "Action");
            var elapsedIndex = GetColumnIndex(table, "Elapsed");

            foreach (var row in table.Rows)
            {
                yield return new MeasureData
                {
                    Id = row[idIndex]?.ToString()!,
                    Fingerprint = row[fingerprintIndex]?.ToString()!,
                    TimeGenerated = GetDateTime(row, timeIndex),

                    Message = row[messageIndex]?.ToString()!,
                    Environment = row[environmentIndex]?.ToString()!,
                    Application = row[applicationIndex]?.ToString()!,

                    Action = row[actionIndex]?.ToString()!,
                    Elapsed = GetTimeSpan(row, elapsedIndex)
                };
            }
        }
    }

    public async IAsyncEnumerable<CountData> GetCountAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        var items = await GetCountInternalAsync(context, environment, timeSpan).ToArrayAsync();
        var duplicates = items.GroupBy(x => x.Id).Where(x => x.Count() > 1).ToArray();
        if (duplicates.Any())
        {
            //TODO: Fix error with Id, it should always be unique.
            Debugger.Break();
        }

        foreach (var item in items)
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<CountData> GetCountInternalAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

        var envFilter = environment ?? string.Empty;

        var query = @$"
AppTraces
| extend _p = todynamic(Properties)
| extend
    Method = tostring(parse_json(tostring(_p[""Details""]))[""Method""])
| where Method == ""Count""
| extend
    Action = tostring(_p[""Action""]),
    ApplicationName = coalesce(tostring(_p[""ApplicationName""]), tostring(AppRoleName)),
    Environment = tostring(_p[""AspNetCoreEnvironment""]),
    Count = tostring(parse_json(tostring(_p[""Details""]))[""Count""])
| extend
    Id = tostring(hash(strcat(TimeGenerated, ""|"", OperationId, ""|"", Message))),
    Fingerprint = tostring(hash(strcat(""Measure|"", Action)))
| project
    TimeGenerated,
    Id,
    Fingerprint,
    Message,
    Environment,
    ApplicationName,
    Action,
    Count
| order by TimeGenerated desc";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        foreach (var table in response.Value.AllTables)
        {
            var timeIndex = GetColumnIndex(table, "TimeGenerated");
            var idIndex = GetColumnIndex(table, "Id");
            var fingerprintIndex = GetColumnIndex(table, "Fingerprint");
            var messageIndex = GetColumnIndex(table, "Message");
            var environmentIndex = GetColumnIndex(table, "Environment");
            var applicationIndex = GetColumnIndex(table, "ApplicationName");
            var actionIndex = GetColumnIndex(table, "Action");
            var countIndex = GetColumnIndex(table, "Count");

            foreach (var row in table.Rows)
            {
                yield return new CountData
                {
                    Id = row[idIndex]?.ToString()!,
                    Fingerprint = row[fingerprintIndex]?.ToString()!,
                    TimeGenerated = GetDateTime(row, timeIndex),

                    Message = row[messageIndex]?.ToString()!,
                    Environment = row[environmentIndex]?.ToString()!,
                    Application = row[applicationIndex]?.ToString()!,

                    Action = row[actionIndex]?.ToString()!,
                    Count = GetInt(row, countIndex)
                };
            }
        }
    }

    //--->

    private static async Task GetSchema(TimeSpan timeSpan, LogsQueryClient client, string workspaceId)
    {
        await PrintSchema("AppExceptions", client, workspaceId, timeSpan);
        Console.WriteLine();
        await PrintSchema("AppTraces", client, workspaceId, timeSpan);
        Console.WriteLine();
        await PrintSchema("AppRequests", client, workspaceId, timeSpan);
    }

    private static async Task PrintSchema(
        string tableName,
        LogsQueryClient client,
        string workspaceId,
        TimeSpan timeSpan)
    {
        Console.WriteLine($"{tableName}:");

        var query = $@"
{tableName}
| getschema";

        var response = await client.QueryWorkspaceAsync(
            workspaceId,
            query,
            new QueryTimeRange(timeSpan));

        var table = response.Value.Table;

        // getschema returns rows where each row describes ONE column
        // Row layout:
        // [0] ColumnName
        // [1] ColumnOrdinal
        // [2] DataType
        // [3] ColumnType
        foreach (var row in table.Rows)
        {
            var columnName = row[0]?.ToString();
            var dataType = row[2]?.ToString();

            Console.WriteLine($"  {columnName} ({dataType})");
        }
    }

    //TODO: --> Revisit

    public async IAsyncEnumerable<SummaryData> GetSummaryAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan, SeverityLevel minSeverityLevel)
    {
        var client = GetClient(context);
        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

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
        var response = await client.QueryWorkspaceAsync(workspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var row in table.Rows)
            {
                yield return new SummaryData
                {
                    SummaryId = BuildSummaryIdentifier(row, LogSource.Exception),
                    Application = row["AppName"].ToString(),
                    Environment = row["Environment"]?.ToString(),
                    Message = row["Message"].ToString(),
                    SeverityLevel = (SeverityLevel)Convert.ToInt32(row["SeverityLevel"]),
                    LastSeen = DateTime.TryParse(row["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
                    IssueCount = Convert.ToInt32(row["IssueCount"]),
                    Type = LogSource.Exception
                };
            }
        }

        //NOTE: Pull data from traces
        query = @$"AppTraces
| where SeverityLevel >= 0
| where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
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
        response = await client.QueryWorkspaceAsync(workspaceId, query, new QueryTimeRange(timeSpan, DateTimeOffset.Now));
        foreach (var table in response.Value.AllTables)
        {
            foreach (var row in table.Rows)
            {
                yield return new SummaryData
                {
                    SummaryId = BuildSummaryIdentifier(row, LogSource.Trace),
                    Application = row["AppName"].ToString(),
                    Environment = row["Environment"]?.ToString(),
                    Message = Convert.ToString(row["Message"]),
                    SeverityLevel = (SeverityLevel)Convert.ToInt32(row["SeverityLevel"]),
                    LastSeen = DateTime.TryParse(row["LastSeen"].ToString(), out var lastEntry) ? lastEntry : null,
                    IssueCount = Convert.ToInt32(row["IssueCount"]),
                    Type = LogSource.Trace
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

//    public async IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
//    {
//        var client = GetClient(context);
//        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

//        if (!Guid.TryParse(workspaceId, out _))
//        {
//            throw new InvalidOperationException("workspaceId must be a Log Analytics Workspace GUID");
//        }

//        var environmentFilter = string.IsNullOrWhiteSpace(environment)
//            ? string.Empty
//            : $"| where AspNetCoreEnvironment == \"{environment}\"";

//        var detailQuery = @$"
//AppTraces
//| extend
//    DetailsRaw = column_ifexists(""customDimensions.Details"", """"),
//    Action = column_ifexists(""customDimensions.Action"", """"),
//    ApplicationName = column_ifexists(""customDimensions.ApplicationName"", """"),
//    AspNetCoreEnvironment = column_ifexists(""customDimensions.AspNetCoreEnvironment"", """")
//| extend Details = parse_json(DetailsRaw)
//| extend Method = tostring(Details.Method)
//| where Method == ""Measure""
//{environmentFilter}
//| extend
//    ElapsedRaw = extract(@""in ([0-9:\.]+) ms"", 1, Message)
//| project
//    TimeGenerated,
//    Action,
//    ApplicationName,
//    AspNetCoreEnvironment,
//    ElapsedRaw
//| order by TimeGenerated desc";

//        var response = await client.QueryWorkspaceAsync(
//            workspaceId,
//            detailQuery,
//            new QueryTimeRange(timeSpan));

//        var table = response.Value.Table;

//        var timeIndex = GetColumnIndex(table, "TimeGenerated");
//        var actionIndex = GetColumnIndex(table, "Action");
//        var applicationNameIndex = GetColumnIndex(table, "ApplicationName");
//        var environmentIndex = GetColumnIndex(table, "AspNetCoreEnvironment");
//        var elapsedIndex = GetColumnIndex(table, "ElapsedRaw");

//        foreach (var row in table.Rows)
//        {
//            if (row == null)
//            {
//                continue;
//            }

//            yield return new MeasureData
//            {
//                TimeGenerated = GetDateTimeOffset(row, timeIndex),
//                Action = GetString(row, actionIndex),
//                ApplicationName = GetString(row, applicationNameIndex),
//                Environment = GetString(row, environmentIndex),
//                Elapsed = GetTimeSpan(row, elapsedIndex)
//            };
//        }

//        static int GetColumnIndex(LogsTable table, string name)
//        {
//            for (var i = 0; i < table.Columns.Count; i++)
//            {
//                if (string.Equals(table.Columns[i].Name, name, StringComparison.Ordinal))
//                {
//                    return i;
//                }
//            }

//            throw new InvalidOperationException($"Column '{name}' was not returned by the query.");
//        }

//        static string GetString(IReadOnlyList<object> row, int index)
//        {
//            return row[index]?.ToString();
//        }

//        static DateTimeOffset GetDateTimeOffset(IReadOnlyList<object> row, int index)
//        {
//            var value = row[index];

//            if (value is DateTimeOffset dto)
//            {
//                return dto;
//            }

//            if (value is DateTime dt)
//            {
//                return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
//            }

//            return DateTimeOffset.Parse(value.ToString());
//        }

//        static TimeSpan GetTimeSpan(IReadOnlyList<object> row, int index)
//        {
//            var value = row[index]?.ToString();

//            if (string.IsNullOrWhiteSpace(value))
//            {
//                return TimeSpan.Zero;
//            }

//            if (TimeSpan.TryParse(value, out var result))
//            {
//                return result;
//            }

//            throw new InvalidOperationException($"Invalid TimeSpan value '{value}'.");
//        }
//    }

    //    public async IAsyncEnumerable<MeasureData> GetMeasureAsync(IApplicationInsightsContext context, string environment, TimeSpan timeSpan)
    //    {
    //        var client = GetClient(context);
    //        var workspaceId = context?.WorkspaceId ?? _options.WorkspaceId;

    //        if (!Guid.TryParse(workspaceId, out _)) throw new InvalidOperationException("workspaceId must be a Log Analytics Workspace GUID");

    //        /*
    //        | where Properties['AspNetCoreEnvironment'] == '{environment}' or isempty(Properties['AspNetCoreEnvironment'])
    //         */
    //        var detailQuery = @$"
    //AppTraces
    //| extend
    //    DetailsRaw = column_ifexists(""customDimensions.Details"", """"),
    //    Action = column_ifexists(""customDimensions.Action"", """"),
    //    ApplicationName = column_ifexists(""customDimensions.ApplicationName"", """"),
    //    AspNetCoreEnvironment = column_ifexists(""customDimensions.AspNetCoreEnvironment"", """")
    //| extend Details = parse_json(DetailsRaw)
    //| extend Method = tostring(Details.Method)
    //| where Method == ""Measure""
    //| extend
    //    Elapsed = extract(@""in ([0-9:\.]+) ms"", 1, Message)
    //| project
    //    TimeGenerated,
    //    Action,
    //    ApplicationName,
    //    AspNetCoreEnvironment,
    //    Elapsed
    //| order by TimeGenerated desc";

    //        var response = await client.QueryWorkspaceAsync(
    //            workspaceId,
    //            detailQuery,
    //            new QueryTimeRange(timeSpan));

    //        var table = response.Value.Table;

    //        var timeIndex = table.Columns.FindIndex(c => c.Name == "TimeGenerated");
    //        var actionIndex = table.Columns.FindIndex(c => c.Name == "Action");
    //        var applicationNameIndex = table.Columns.FindIndex(c => c.Name == "ApplicationName");
    //        var environmentIndex = table.Columns.FindIndex(c => c.Name == "AspNetCoreEnvironment");
    //        var elapsedIndex = table.Columns.FindIndex(c => c.Name == "Elapsed");

    //        foreach (var row in table.Rows)
    //        {
    //            if (row == null)
    //            {
    //                continue;
    //            }

    //            yield return new MeasureData
    //            {
    //                TimeGenerated = (DateTimeOffset)row[timeIndex],
    //                Action = row[actionIndex]?.ToString(),
    //                ApplicationName = row[applicationNameIndex]?.ToString(),
    //                AspNetCoreEnvironment = row[environmentIndex]?.ToString(),
    //                Elapsed = row[elapsedIndex]?.ToString()
    //            };
    //        }
    //    }

    private static string BuildItemIdentifier(LogsTableRow x, LogSource type)
    {
        switch (type)
        {
            case LogSource.Exception:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new ItemIdentifier
                {
                    Type = type,
                    Identifier = x["Id"]?.ToString()
                }));
            case LogSource.Trace:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new ItemIdentifier
                {
                    Type = type,
                    Identifier = x["Id"].ToString(),
                }));
            case LogSource.Request:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new ItemIdentifier
                {
                    Type = type,
                    Identifier = x["Id"].ToString(),
                }));
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static string BuildSummaryIdentifier(LogsTableRow x, LogSource type)
    {
        switch (type)
        {
            case LogSource.Exception:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["ProblemId"]?.ToString(),
                    Application = x["AppName"]?.ToString()
                }));
            case LogSource.Trace:
                return Base64UrlHelper.EncodeToBase64Url(JsonSerializer.Serialize(new SummaryDataIdentifier
                {
                    Type = type,
                    Identifier = x["ProblemId"].ToString(),
                    Application = x["AppName"].ToString()
                }));
            case LogSource.Request:
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

    private LogsQueryClient GetClient(IApplicationInsightsContext context = null)
    {
        var clientSecret = context?.ClientSecret ?? _options.ClientSecret;
        var tenantId = context?.TenantId ?? _options.TenantId;
        var clientId = context?.ClientId ?? _options.ClientId;

        if (string.IsNullOrEmpty(clientSecret)) throw new InvalidOperationException($"No {nameof(ApplicationInsightsOptions.ClientSecret)} has been configured.");
        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
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
            case LogSource.Exception:
                detailQuery = @$"AppExceptions
| where tostring(hash(strcat(ProblemId, Properties['OriginalFormat']))) == '{summaryIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(coalesce(Message, OuterMessage)))))
| extend ProblemId = tostring(hash(strcat(ProblemId, Properties['OriginalFormat'])))
| order by TimeGenerated desc";
                break;
            case LogSource.Trace:
                detailQuery = @$"AppTraces
| where tostring(hash(tostring(Properties['OriginalFormat']))) == '{summaryIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(Message))))
| extend ProblemId = tostring(hash(tostring(Properties['OriginalFormat'])))
| order by TimeGenerated desc";
                break;
            case LogSource.Request:
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
            case LogSource.Exception:
                detailQuery = @$"AppExceptions
| where tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(coalesce(Message, OuterMessage))))) == '{itemIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(coalesce(Message, OuterMessage)))))
| extend ProblemId = tostring(hash(strcat(ProblemId, Properties['OriginalFormat'])))
| order by TimeGenerated desc";
                break;
            case LogSource.Trace:
                detailQuery = @$"AppTraces
| where tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(Message)))) == '{itemIdentifier.Identifier}'
| extend Id = tostring(hash(strcat(tostring(TimeGenerated), '|', tostring(Message))))
| extend ProblemId = tostring(hash(tostring(Properties['OriginalFormat'])))
| order by TimeGenerated desc | take 1";
                break;
            case LogSource.Request:
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
                Id = BuildItemIdentifier(row, LogSource.Exception),
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

    public async Task<bool> CanConnectAsync(IApplicationInsightsContext context)
    {
        try
        {
            var client = GetClient(context);
            var detailQuery = "AppTraces";
            _ = await client.QueryWorkspaceAsync(context.WorkspaceId, detailQuery, new QueryTimeRange(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now));

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
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

    static int GetColumnIndex(LogsTable table, string name)
    {
        for (var i = 0; i < table.Columns.Count; i++)
        {
            if (string.Equals(table.Columns[i].Name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Column '{name}' not found.");
    }

    static DateTime GetDateTime(IReadOnlyList<object> row, int index)
    {
        var value = row[index];

        if (value is DateTimeOffset dto)
        {
            return dto.UtcDateTime;
        }

        if (value is DateTime dt)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        return DateTime.Parse(value.ToString()!);
    }

    static int GetInt(IReadOnlyList<object> row, int index)
    {
        var value = row[index];

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return (int)l;
        }

        return int.Parse(value.ToString()!);
    }

    static string BuildItemIdentifier(
        IReadOnlyList<object> row,
        int idIndex,
        LogSource type)
    {
        var identifier = row[idIndex]?.ToString();

        return Base64UrlHelper.EncodeToBase64Url(
            JsonSerializer.Serialize(
                new ItemIdentifier
                {
                    Type = type,
                    Identifier = identifier
                }
            )
        );
    }

    static TimeSpan GetTimeSpan(IReadOnlyList<object> row, int index)
    {
        var value = row[index];

        if (value is TimeSpan ts)
        {
            return ts;
        }

        return TimeSpan.Parse(value?.ToString() ?? "00:00:00");
    }
}