using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Mira.Features.StreamChecker;

public class BroadcastBoxClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BroadcastBoxClient> _logger;

    public BroadcastBoxClient(IHttpClientFactory httpClientFactory, ILogger<BroadcastBoxClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<KeyStatus>> GetStreamKeysAsync(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return [];
        }

        var client = _httpClientFactory.CreateClient(host);
        
        var result = await client.GetAsync($"{host}/api/status");
        if (!result.IsSuccessStatusCode)
        {
            _logger.LogError("[BROADCASTBOXCLIENT] Failed to make get request to {Host}/api/status. Status code: {Status}", host, result.StatusCode);
            return [];
        }

        var statuses = await result.Content.ReadFromJsonAsync<KeyStatus[]>();
        if (statuses is null)
        {
            var rawResponse = await result.Content.ReadAsStringAsync();
            _logger.LogError("[BROADCASTBOXCLIENT] Failed to extract KeyStatus models from JSON. Raw response: {Response}", rawResponse);
        }

        return statuses ?? [];
    }
}