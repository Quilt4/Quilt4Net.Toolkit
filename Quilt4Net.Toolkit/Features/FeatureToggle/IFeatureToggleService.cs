//using System.Net.Http.Json;
//using Microsoft.Extensions.Options;

//namespace Quilt4Net.Toolkit.Features.FeatureToggle;

//public interface IFeatureToggleService
//{
//    ValueTask<bool> GetToggleAsync(string key, bool fallback = false);
//    ValueTask<T> GetValueAsync<T>(string key, T fallback = default);
//}

//internal class FeatureToggleService : IFeatureToggleService
//{
//    public FeatureToggleService(IOptions<Quilt4NetApiOptions> options)
//    {
//    }

//    public async ValueTask<bool> GetToggleAsync(string key, bool fallback = false)
//    {
//        return await MakeCallAsync<bool>(key);
//    }

//    public async ValueTask<T> GetValueAsync<T>(string key, T fallback = default)
//    {
//        return await MakeCallAsync<T>(key);
//    }

//    private async Task<T> MakeCallAsync<T>(string key)
//    {
//        //TODO: Handle cache

//        //TODO: Provide application information...


//        using var client = new HttpClient();
//        client.DefaultRequestHeaders.Add("abc", "123"); //TODO: Add API-Key
//        client.BaseAddress = new Uri("https://localhost:7129/"); //TODO: Configure this
//        var response = await client.GetAsync($"Api/FeatureToggle/{key}");
//        response.EnsureSuccessStatusCode();

//        var result = await response.Content.ReadFromJsonAsync<FeatureToggleDto>();

//        var value = (T)Convert.ChangeType(result.Value, typeof(T));
//        return value;
//    }
//}

//public record FeatureToggleDto
//{
//    public required string Value { get; init; }
//}