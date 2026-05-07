using System.Text.Json;
using Quilt4Net.Toolkit.Features.ApplicationInsights;
using Tharga.Mcp;

namespace Quilt4Net.Toolkit.Mcp;

/// <summary>
/// Read-only browsing surfaces for Application Insights. Lives on
/// <see cref="McpScope.System"/>; exposes environment names and recent
/// summary buckets. The actual log-content tools are on
/// <see cref="ApplicationInsightsToolProvider"/>.
/// </summary>
public sealed class ApplicationInsightsResourceProvider : IMcpResourceProvider
{
    internal const string EnvironmentsUri = "quilt4net://environments";
    internal const string SummariesUri = "quilt4net://summaries";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IApplicationInsightsService _ais;
    private readonly Quilt4NetMcpOptions _options;

    public ApplicationInsightsResourceProvider(IApplicationInsightsService ais, Quilt4NetMcpOptions options)
    {
        _ais = ais;
        _options = options;
    }

    public McpScope Scope => McpScope.System;

    public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken)
    {
        IReadOnlyList<McpResourceDescriptor> resources =
        [
            new McpResourceDescriptor
            {
                Uri = EnvironmentsUri,
                Name = "Quilt4Net environments",
                Description = "Environment names seen in the configured Application Insights workspace.",
                MimeType = "application/json"
            },
            new McpResourceDescriptor
            {
                Uri = SummariesUri,
                Name = "Quilt4Net summaries",
                Description = "Recent log summary buckets across all environments (default lookback).",
                MimeType = "application/json"
            }
        ];
        return Task.FromResult(resources);
    }

    public async Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken)
    {
        return uri switch
        {
            EnvironmentsUri => await ReadEnvironmentsAsync(cancellationToken),
            SummariesUri => await ReadSummariesAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unknown resource: {uri}")
        };
    }

    private async Task<McpResourceContent> ReadEnvironmentsAsync(CancellationToken cancellationToken)
    {
        var items = await _ais.GetEnvironments(null)
            .Select(e => new { value = e.Value, label = e.Label })
            .ToArrayAsync(cancellationToken);

        return new McpResourceContent
        {
            Uri = EnvironmentsUri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { environments = items }, JsonOptions)
        };
    }

    private async Task<McpResourceContent> ReadSummariesAsync(CancellationToken cancellationToken)
    {
        var items = await _ais.GetSummaries(null, null, _options.DefaultLookback)
            .Take(100)
            .ToArrayAsync(cancellationToken);

        return new McpResourceContent
        {
            Uri = SummariesUri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new
            {
                lookbackHours = (int)_options.DefaultLookback.TotalHours,
                count = items.Length,
                summaries = items
            }, JsonOptions)
        };
    }
}
