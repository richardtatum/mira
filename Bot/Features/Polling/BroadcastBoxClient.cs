using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Mira.Features.Polling.Models;

namespace Mira.Features.Polling;

public class BroadcastBoxClient(IHttpClientFactory httpClientFactory, ILogger<BroadcastBoxClient> logger)
{
    public async Task<KeyStatus[]> GetStreamsAsync(string hostUrl)
    {
        if (string.IsNullOrWhiteSpace(hostUrl))
        {
            return [];
        }

        var client = httpClientFactory.CreateClient(hostUrl);
        
        var result = await client.GetAsync($"{hostUrl}/api/status");
        if (!result.IsSuccessStatusCode)
        {
            logger.LogError("[BROADCASTBOX-CLIENT] Failed to make get request to {Host}/api/status. Status code: {Status}", hostUrl, result.StatusCode);
            return [];
        }

        var statuses = await result.Content.ReadFromJsonAsync<KeyStatus[]>();
        if (statuses is null)
        {
            var rawResponse = await result.Content.ReadAsStringAsync();
            logger.LogError("[BROADCASTBOX-CLIENT] Failed to extract KeyStatus models from JSON. Raw response: {Response}", rawResponse);
        }

        return statuses ?? [];
    }
}