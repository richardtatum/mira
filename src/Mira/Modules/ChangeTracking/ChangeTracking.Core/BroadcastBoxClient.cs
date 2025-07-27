using System.Net.Http.Json;
using ChangeTracking.Core.Models;
using Microsoft.Extensions.Logging;
using Host = Shared.Core.Models.Host;

namespace ChangeTracking.Core;

internal class BroadcastBoxClient(IHttpClientFactory httpClientFactory, ILogger<BroadcastBoxClient> logger)
{
    internal async Task<KeySummary[]> GetStreamsAsync(Host host)
    {
        if (string.IsNullOrWhiteSpace(host.Url))
        {
            return [];
        }

        var client = httpClientFactory.CreateClient(host.Url);
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"{host.Url}/api/status");
        if (!string.IsNullOrWhiteSpace(host.AuthHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", host.AuthHeader);
        }
        
        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("[BROADCASTBOX-CLIENT] Failed to make get request to {Host}/api/status. Status code: {Status}", host.Url, response.StatusCode);
            return [];
        }

        var statuses = await response.Content.ReadFromJsonAsync<KeySummary[]>();
        if (statuses is null)
        {
            var rawResponse = await response.Content.ReadAsStringAsync();
            logger.LogError("[BROADCASTBOX-CLIENT] Failed to extract KeyStatus models from JSON. Raw response: {Response}", rawResponse);
        }

        return statuses ?? [];
    }
}