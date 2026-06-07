using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Features.Content.Pages;

/// <summary>
/// HTTP wire for <see cref="IContentPageReader"/>. Reuses the same named HttpClient + API key
/// that snippet content uses — one factory client, correlation id propagated. No cache layer
/// yet (Phase 2 keeps page fetches simple); promote-and-stale behaviour mirrors snippet content
/// so adding a TTL cache later is mechanical.
/// </summary>
internal sealed class RemoteContentPageReader : IContentPageReader
{
    private readonly Features.FeatureToggle.EnvironmentName _environmentName;
    private readonly ContentOptions _contentOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RemoteContentPageReader> _logger;

    public RemoteContentPageReader(
        Features.FeatureToggle.EnvironmentName environmentName,
        IOptions<ContentOptions> contentOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<RemoteContentPageReader> logger)
    {
        _environmentName = environmentName;
        _contentOptions = contentOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ContentPageDto> GetBySlugAsync(string slug, Guid languageKey, string application = null)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        if (string.IsNullOrEmpty(_contentOptions.ApiKey))
        {
            // Same posture as RemoteContentCallService: no key => no remote calls; caller renders
            // an empty / not-found state without a network round-trip.
            return null;
        }

        var effectiveApplication = application ?? _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;

        try
        {
            using var cts = new CancellationTokenSource(_contentOptions.HttpTimeout);
            using var client = _httpClientFactory.CreateClient(Features.Content.RemoteContentCallService.HttpClientName);

            var query = $"slug={WebUtility.UrlEncode(slug)}&env={WebUtility.UrlEncode(_environmentName?.Name ?? "")}&languageKey={languageKey}&application={WebUtility.UrlEncode(effectiveApplication ?? "")}";
            var response = await client.GetAsync($"Api/ContentPage?{query}", cts.Token);

            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch content page '{Slug}'. Response was {StatusCode} {ReasonPhrase}.",
                    slug, response.StatusCode, response.ReasonPhrase);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ContentPageDto>(cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HTTP request timed out fetching content page '{Slug}' after {Timeout}ms.",
                slug, _contentOptions.HttpTimeout.TotalMilliseconds);
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Falling back to null for slug '{Slug}'.", e.Message, slug);
            return null;
        }
    }

    public async Task<IReadOnlyList<ContentMenuItemDto>> GetTreeAsync(Guid languageKey, string application = null)
    {
        if (string.IsNullOrEmpty(_contentOptions.ApiKey)) return [];

        var effectiveApplication = application ?? _contentOptions.Application ?? Assembly.GetEntryAssembly()?.GetName()?.Name;

        try
        {
            using var cts = new CancellationTokenSource(_contentOptions.HttpTimeout);
            using var client = _httpClientFactory.CreateClient(Features.Content.RemoteContentCallService.HttpClientName);

            var query = $"env={WebUtility.UrlEncode(_environmentName?.Name ?? "")}&languageKey={languageKey}&application={WebUtility.UrlEncode(effectiveApplication ?? "")}";
            var response = await client.GetAsync($"Api/ContentPage/tree?{query}", cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch content page tree. Response was {StatusCode} {ReasonPhrase}.",
                    response.StatusCode, response.ReasonPhrase);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<ContentMenuItemDto[]>(cancellationToken: cts.Token) ?? [];
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HTTP request timed out fetching content page tree after {Timeout}ms.",
                _contentOptions.HttpTimeout.TotalMilliseconds);
            return [];
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message} Falling back to empty menu tree.", e.Message);
            return [];
        }
    }
}
