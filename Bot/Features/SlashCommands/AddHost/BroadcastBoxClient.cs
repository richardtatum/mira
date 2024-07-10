using Microsoft.Extensions.Logging;

namespace Mira.Features.SlashCommands.AddHost;

public class BroadcastBoxClient(IHttpClientFactory httpClientFactory, ILogger<StreamChecker.BroadcastBoxClient> logger)
{
    public async Task<bool> IsVerifiedBroadcastBoxHostAsync(string hostUrl)
    {
        if (string.IsNullOrWhiteSpace(hostUrl))
        {
            return false;
        }

        var client = httpClientFactory.CreateClient(hostUrl);
        // Set a timeout in case the URL can't be contacted at all
        client.Timeout = TimeSpan.FromSeconds(1);

        try
        {
            var result = await client.GetAsync($"{hostUrl}/api/status");
            return result.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fucked: {ex.Message}");
            return false;
        }
    }
}