using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quilt4Net.Toolkit.Features.FeatureToggle;

namespace Quilt4Net.Toolkit.Features.Content;

/// <inheritdoc cref="IContentWriteService"/>
internal sealed class ContentWriteService : IContentWriteService
{
    private readonly EnvironmentName _environmentName;
    private readonly ContentOptions _contentOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContentWriteService> _logger;

    public ContentWriteService(EnvironmentName environmentName, IOptions<ContentOptions> contentOptions,
        IHttpClientFactory httpClientFactory, ILogger<ContentWriteService> logger)
    {
        _environmentName = environmentName;
        _contentOptions = contentOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ContentImportResult> ImportAsync(IReadOnlyCollection<ContentImportItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) return new ContentImportResult();

        if (string.IsNullOrEmpty(_contentOptions.ApiKey))
        {
            throw new InvalidOperationException(
                "No API key is configured, so content cannot be written. Set ContentOptions.ApiKey to a key holding the 'content:write' scope.");
        }

        var request = new ContentImportRequest
        {
            Environment = _environmentName.Name,
            Items = items.ToArray(),
        };

        // No timeout of its own beyond the caller's token. This is not the read path: a deliberate
        // write of 83 keys across every language is allowed to take as long as it takes, and cutting
        // it off mid-way would leave exactly the partially-applied state the server's all-or-nothing
        // gate exists to avoid.
        using var client = _httpClientFactory.CreateClient(RemoteContentCallService.HttpClientName);
        var response = await client.PostAsJsonAsync("Api/Content/import", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Content import failed: {StatusCode} {ReasonPhrase}. {Body}", response.StatusCode, response.ReasonPhrase, body);

            // Thrown, not swallowed into an empty result. Every other path in this client degrades
            // gracefully because a reader showing slightly stale text is better than a broken page;
            // a write that reports success while changing nothing is the opposite — the caller would
            // record the change as done and nobody would look again.
            throw new HttpRequestException(
                $"Content import failed with {(int)response.StatusCode} {response.ReasonPhrase}. {body}".Trim());
        }

        var result = await response.Content.ReadFromJsonAsync<ContentImportResult>(cancellationToken: cancellationToken)
                     ?? new ContentImportResult();

        if (result.IgnoredLanguages.Count > 0)
        {
            // Warning rather than Debug: this is the failure that otherwise looks like a success.
            _logger.LogWarning("Content import ignored {Count} unrecognised language name(s): {Languages}. Those translations were not written.",
                result.IgnoredLanguages.Count, string.Join(", ", result.IgnoredLanguages));
        }

        _logger.LogInformation("Content import wrote {Values} value(s) across {Keys} key(s).", result.ValuesWritten, result.KeysWritten);
        return result;
    }
}
