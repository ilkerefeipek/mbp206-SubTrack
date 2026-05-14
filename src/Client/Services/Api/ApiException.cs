using System.Net;
using System.Text.Json;

namespace SubTrack.Client.Services.Api;

public sealed class ApiException : Exception
{
    public ApiException(
        HttpStatusCode statusCode,
        string title,
        string? detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(detail ?? title)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Errors = errors;
    }

    public HttpStatusCode StatusCode { get; }
    public string Title { get; }
    public string? Detail { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public bool IsValidation => StatusCode == HttpStatusCode.BadRequest;
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsConflict => StatusCode == HttpStatusCode.Conflict;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsRateLimited => (int)StatusCode == 429;

    public static async Task<ApiException> FromResponseAsync(
        HttpResponseMessage response,
        CancellationToken ct = default)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
        {
            return new ApiException(response.StatusCode, response.ReasonPhrase ?? "Hata", null);
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var title = TryGetString(root, "title") ?? response.ReasonPhrase ?? "Hata";
            var detail = TryGetString(root, "detail");
            Dictionary<string, string[]>? errors = null;

            if (root.TryGetProperty("errors", out var errEl) && errEl.ValueKind == JsonValueKind.Object)
            {
                errors = new Dictionary<string, string[]>();
                foreach (var prop in errEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        errors[prop.Name] = prop.Value.EnumerateArray()
                            .Select(e => e.GetString() ?? "")
                            .ToArray();
                    }
                }
            }

            return new ApiException(response.StatusCode, title, detail, errors);
        }
        catch (JsonException)
        {
            return new ApiException(response.StatusCode, response.ReasonPhrase ?? "Hata", body);
        }
    }

    private static string? TryGetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
