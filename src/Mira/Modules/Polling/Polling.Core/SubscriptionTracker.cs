using Microsoft.Extensions.Logging;

namespace Polling.Core;

public class SubscriptionTracker(ILogger logger)
{
    private readonly Dictionary<string, CancellationTokenSource> _subscriptions = new();

    public CancellationToken Register(string hostUrl, CancellationToken baseCancellationToken)
    {
        var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(baseCancellationToken);
        _subscriptions.Add(hostUrl, cancellationSource);
        logger.LogInformation("[SUBSCRIPTION-TRACKER][{Host}] Subscription registered.", hostUrl);
        return cancellationSource.Token;
    }
    
    public async Task UnsubscribeAsync(string hostUrl)
    {
        if (!_subscriptions.TryGetValue(hostUrl, out var cancellationTokenSource))
        {
            logger.LogError("[SUBSCRIPTION-TRACKER] Unable to find subscribed host with url: {Host}.", hostUrl);
            return;
        }

        await cancellationTokenSource.CancelAsync();
        _subscriptions.Remove(hostUrl);
    }

    public bool Contains(string hostUrl) => _subscriptions.ContainsKey(hostUrl);

    public async Task CleanupAsync(IEnumerable<string> activeHostUrls)
    {
        await CleanupStaleHostsAsync(activeHostUrls);
        CleanupCancelledHosts();
    }

    private void CleanupCancelledHosts()
    {
        var staleHosts = _subscriptions
            .Where(subscription => subscription.Value.IsCancellationRequested);
        
        foreach (var staleHost in staleHosts)
        {
            logger.LogInformation("[SUBSCRIPTION-TRACKER][{Host}] Cancellation token has been triggered. Removing from subscribed hosts.",
                staleHost.Key);
            _subscriptions.Remove(staleHost.Key);
        }
    }

    private Task CleanupStaleHostsAsync(IEnumerable<string> activeHostUrls)
    {
        var cancellationTasks = _subscriptions
            .Where(subscription => !activeHostUrls.Contains(subscription.Key))
            .Select(entry =>
            {
                logger.LogInformation("[SUBSCRIPTION-TRACKER][{Host}] Host no longer registered in the database. Cancelling.",
                    entry.Key);
                return entry.Value.CancelAsync();
            });

        return Task.WhenAll(cancellationTasks);
    }
}