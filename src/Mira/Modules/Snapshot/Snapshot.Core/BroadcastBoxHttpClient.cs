using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Snapshot.Core;

public class BroadcastBoxHttpClient(HttpClient httpClient, ILogger<BroadcastBoxHttpClient> logger) // : IWebRTCExchangeClient???
{
    public async Task<string?> ExchangeOfferAsync(string hostUrl, string bearerToken, string offerSdp,
        CancellationToken cancellationToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/sdp"));

        var content = new StringContent(offerSdp, Encoding.UTF8, "application/sdp");

        var response = await httpClient.PostAsync($"{hostUrl}/api/whep", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogCritical("[SNAPSHOT][{Host}][{Key}] Offer request failed. Status: {Status}, Reason: {Reason}", hostUrl, bearerToken, response.StatusCode, response.ReasonPhrase);
            return null;
        }

        logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Offer request succeeded.", hostUrl, bearerToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}