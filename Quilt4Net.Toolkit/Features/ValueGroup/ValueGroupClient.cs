using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quilt4Net.Toolkit.Features.ValueGroup;

internal class ValueGroupClient : IValueGroupClient
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly ValueGroupClientOptions _options;
    private readonly ILogger<ValueGroupClient> _logger;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private ValueGroupBundle _cached;
    private DateTime _cachedAt = DateTime.MinValue;
    private volatile bool _refreshInProgress;

    public ValueGroupClient(IOptions<ValueGroupClientOptions> options, ILogger<ValueGroupClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("ValueGroupClientOptions.ApiKey is required.");
        if (string.IsNullOrWhiteSpace(_options.GroupId))
            throw new InvalidOperationException("ValueGroupClientOptions.GroupId is required.");
        if (!Uri.TryCreate(_options.Quilt4NetAddress, UriKind.Absolute, out _))
            throw new InvalidOperationException($"ValueGroupClientOptions.Quilt4NetAddress '{_options.Quilt4NetAddress}' is not an absolute URI.");
    }

    public async Task<ValueGroupBundle> GetAsync(CancellationToken cancellationToken = default)
    {
        var ttl = _options.Ttl ?? DefaultTtl;
        var cached = _cached;
        var age = DateTime.UtcNow - _cachedAt;

        if (cached != null && age < ttl) return cached;

        // Stale-while-revalidate: hand back the stale bundle and refresh in the background.
        if (cached != null)
        {
            StartBackgroundRefresh();
            return cached;
        }

        return await FetchAsync(cancellationToken);
    }

    private async Task<ValueGroupBundle> FetchAsync(CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have populated the cache while we waited.
            var cached = _cached;
            var ttl = _options.Ttl ?? DefaultTtl;
            if (cached != null && DateTime.UtcNow - _cachedAt < ttl) return cached;

            using var timeoutCts = new CancellationTokenSource(_options.HttpTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            using var client = CreateHttpClient();

            var response = await client.GetAsync($"Api/ValueGroup/{_options.GroupId}", linkedCts.Token);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Revoked / unbound / wrong scope. Don't fall back to cached data — surface explicitly.
                throw new ValueGroupAuthorizationException(
                    $"Server returned {(int)response.StatusCode} {response.StatusCode} for value group '{_options.GroupId}'. The API key may have been revoked or is not bound to this group.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Unable to fetch value group '{GroupId}'. Response was {StatusCode} {ReasonPhrase}.",
                    _options.GroupId, response.StatusCode, response.ReasonPhrase);
                if (cached != null) return cached;
                response.EnsureSuccessStatusCode(); // throws
            }

            var bundle = await response.Content.ReadFromJsonAsync<ValueGroupBundle>(linkedCts.Token)
                         ?? throw new InvalidOperationException($"Server returned an empty body for value group '{_options.GroupId}'.");

            _cached = bundle;
            _cachedAt = DateTime.UtcNow;
            return bundle;
        }
        catch (ValueGroupAuthorizationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Value group '{GroupId}' fetch timed out after {Timeout}ms.",
                _options.GroupId, _options.HttpTimeout.TotalMilliseconds);
            if (_cached != null) return _cached;
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch value group '{GroupId}': {Message}.", _options.GroupId, e.Message);
            if (_cached != null) return _cached;
            throw;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private void StartBackgroundRefresh()
    {
        if (_refreshInProgress) return;
        _refreshInProgress = true;

        _ = Task.Run(async () =>
        {
            try
            {
                await FetchAsync(CancellationToken.None);
            }
            catch (ValueGroupAuthorizationException ex)
            {
                _logger.LogWarning(ex, "Background refresh for value group '{GroupId}' was denied; stale cache will continue serving until the next foreground fetch.",
                    _options.GroupId);
            }
            catch
            {
                // Already logged inside FetchAsync.
            }
            finally
            {
                _refreshInProgress = false;
            }
        });
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient client = null;
        try
        {
            client = new HttpClient { BaseAddress = new Uri(_options.Quilt4NetAddress) };
            client.DefaultRequestHeaders.Add("X-API-KEY", _options.ApiKey);
            return client;
        }
        catch
        {
            client?.Dispose();
            throw;
        }
    }
}
