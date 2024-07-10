using Microsoft.Extensions.Logging;

namespace Mira.Features.SlashCommands.AddHost;

public class BroadcastBoxClient(IHttpClientFactory httpClientFactory, ILogger<StreamChecker.BroadcastBoxClient> logger)
{
    public async Task<bool> IsVerifiedBroadcastBoxHostAsync(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var client = httpClientFactory.CreateClient(host);
        // Set a timeout in case the URL can't be contacted at all
        client.Timeout = TimeSpan.FromSeconds(1);

        try
        {
            var result = await client.GetAsync($"{host}/api/status");
            return result.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fucked: {ex.Message}");
            return false;
        }
    }
}