using System.Net.Http.Json;
using System.Text.Json;

namespace SubTrack.Client.Services.Api;

public abstract class ApiClientBase(HttpClient http)
{
    protected readonly HttpClient _http = http;

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);
        return await DeserializeOrThrowAsync<T>(resp, ct);
    }

    protected async Task<T> PostAsync<T>(string url, object body, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync(url, body, JsonOptions, ct);
        return await DeserializeOrThrowAsync<T>(resp, ct);
    }

    protected async Task PostAsync(string url, object? body, CancellationToken ct)
    {
        using var resp = body is null
            ? await _http.PostAsync(url, content: null, ct)
            : await _http.PostAsJsonAsync(url, body, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    protected async Task<T> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        using var resp = await _http.PutAsJsonAsync(url, body, JsonOptions, ct);
        return await DeserializeOrThrowAsync<T>(resp, ct);
    }

    protected async Task PutAsync(string url, object? body, CancellationToken ct)
    {
        using var resp = body is null
            ? await _http.PutAsync(url, content: null, ct)
            : await _http.PutAsJsonAsync(url, body, JsonOptions, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    protected async Task DeleteAsync(string url, CancellationToken ct)
    {
        using var resp = await _http.DeleteAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (!resp.IsSuccessStatusCode)
        {
            throw await ApiException.FromResponseAsync(resp, ct);
        }
    }

    private static async Task<T> DeserializeOrThrowAsync<T>(HttpResponseMessage resp, CancellationToken ct)
    {
        if (!resp.IsSuccessStatusCode)
        {
            throw await ApiException.FromResponseAsync(resp, ct);
        }
        var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return data ?? throw new InvalidOperationException("Boş yanıt alındı.");
    }
}
