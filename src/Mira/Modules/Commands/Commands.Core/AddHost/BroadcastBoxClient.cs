using Commands.Core.AddHost.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commands.Core.AddHost;

public class BroadcastBoxClient(
    IHttpClientFactory httpClientFactory,
    ILogger<BroadcastBoxClient> logger,
    IOptions<PollingOptions> options)
{
    public async Task<bool> IsVerifiedBroadcastBoxHostAsync(string hostUrl)
    {
        if (string.IsNullOrWhiteSpace(hostUrl))
        {
            return false;
        }

        var client = httpClientFactory.CreateClient(hostUrl);

        // Set a timeout in case the URL can't be contacted at all
        client.Timeout = TimeSpan.FromSeconds(options.Value.NewHostValidationTimeoutSeconds);

        try
        {
            var result = await client.GetAsync($"{hostUrl}/api/status");
            return result.IsSuccessStatusCode;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning("[BROADCASTBOX-CLIENT][{Host}] Failed to add host as request timed out before it could be completed. Ex: {Ex}", hostUrl, ex.Message);
            return false;
        }
    }
}