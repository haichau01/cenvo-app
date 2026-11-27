using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Cenvo.Tests.Integration;

public static class Http
{
    public static async Task<JsonNode> Get(this HttpClient c, string url) => await Read(await c.GetAsync(url));

    public static async Task<JsonNode> Post(this HttpClient c, string url, object? body = null) =>
        await Read(await c.PostAsJsonAsync(url, body ?? new { }));

    public static async Task<JsonNode> Put(this HttpClient c, string url, object body) =>
        await Read(await c.PutAsJsonAsync(url, body));

    private static async Task<JsonNode> Read(HttpResponseMessage res)
    {
        var text = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException($"{(int)res.StatusCode} {res.RequestMessage?.RequestUri}: {text}", null, res.StatusCode);
        return string.IsNullOrWhiteSpace(text) ? new JsonObject() : JsonNode.Parse(text)!;
    }

    public static Guid Id(this JsonNode n, string prop = "id") => Guid.Parse(n[prop]!.GetValue<string>());
    public static decimal Dec(this JsonNode n, string prop) => n[prop]!.GetValue<decimal>();
    public static string Str(this JsonNode n, string prop) => n[prop]!.GetValue<string>();
    public static JsonArray Arr(this JsonNode n) => n.AsArray();
}
