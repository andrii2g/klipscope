using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KlipScope.Core.Models;

namespace KlipScope.Moonraker.Http;

public sealed class MoonrakerHttpClient(HttpClient httpClient, ResolvedConnectionOptions options)
{
    public async Task<JsonNode?> GetJsonNodeAsync(string pathAndQuery, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, options.BuildMoonrakerUri(pathAndQuery));
        ApplyHeaders(request);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<JsonNode?> PostJsonNodeAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.BuildMoonrakerUri(path));
        ApplyHeaders(request);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await SendAsync(request, cancellationToken);
    }

    private async Task<JsonNode?> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Timeout);
        using var response = await httpClient.SendAsync(request, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new MoonrakerHttpException($"Moonraker request failed with HTTP {(int)response.StatusCode}.", (int)response.StatusCode, body);
        }

        return string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body);
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", options.ApiKey);
        }
    }
}
