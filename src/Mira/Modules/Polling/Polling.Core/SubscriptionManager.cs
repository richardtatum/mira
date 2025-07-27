using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shared.Core.Interfaces;
using Shared.Core.Models;

namespace Polling.Core;

public class SubscriptionManager(IChangeTrackingService service, ILogger<SubscriptionManager> logger) : ISubscriptionManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _subscriptions = new();
    
    public bool IsSubscribed(string hostUrl) => _subscriptions.ContainsKey(hostUrl);

    public void Subscribe(Host host, CancellationToken baseCancellationToken)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(baseCancellationToken);
        _subscriptions[host.Url] = cancellationTokenSource;
        
        _ = Task.Run(() => PollHostAsync(host, cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    private async Task PollHostAsync(Host host, CancellationToken token)
    {
        using var timer = new PeriodicTimer(host.PollInterval);
        do
        {
            try
            {
                await service.ExecuteAsync(host);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SUBSCRIPTIONS][{Host}] Error executing poll. Unsubscribing.", host.Url);
                _subscriptions.TryRemove(host.Url, out _);
                break;
            }
            
            await timer.WaitForNextTickAsync(token);
        } while (!token.IsCancellationRequested);
    }

    public void CleanupSubscriptions(IEnumerable<string> activeHostUrls)
    { 
        var staleHosts = _subscriptions.Keys.Except(activeHostUrls);

        foreach (var host in staleHosts)
        {
            if (!_subscriptions.TryRemove(host, out var tokenSource))
            {
                continue;
            };
            
            logger.LogInformation("[SUBSCRIPTIONS][{Host}] Host no longer active. Unsubscribing.", host);
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }
}