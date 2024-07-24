using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polling.Core.Options;
using Polling.Core.Repositories;
using Shared.Core.Interfaces;
using Host = Polling.Core.Models.Host;

namespace Polling.Core;

public class PollingService(
    QueryRepository query,
    IChangeTrackingService service,
    ILogger<PollingService> logger,
    IOptions<PollingOptions> options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.NewHostIntervalSeconds));

        var subscribedHosts = new Dictionary<string, CancellationTokenSource>();
        do
        {
            logger.LogInformation("[HOST-POLLING] Checking for new hosts...");
            var hosts = await query.GetHostsAsync();
            foreach (var host in hosts)
            {
                if (subscribedHosts.ContainsKey(host.Url))
                {
                    continue;
                }

                logger.LogInformation(
                    "[HOST-POLLING] New host found: {Host}. Creating subscription. Interval: {Seconds}s", host.Url,
                    host.PollIntervalSeconds);

                // Create a child of the provided cancellation token
                var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                subscribedHosts[host.Url] = cancellationSource;
                _ = SubscribeToHostAsync(host, cancellationSource.Token);
            }

            // These are hosts that were previously subscribed but have since been removed from the DB
            var staleHosts = subscribedHosts
                .Where(subscription =>
                    !hosts.Select(host => host.Url).Contains(subscription.Key)
                )
                .Select(entry => (entry.Key, entry.Value))
                .ToArray();

            await CancelSubscriptionsAsync(staleHosts);
            await timer.WaitForNextTickAsync(stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }

    private async Task SubscribeToHostAsync(Host host, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(host.PollInterval);
        do
        {
            await service.ExecuteAsync(host.Url);
            await timer.WaitForNextTickAsync(stoppingToken);
        } while (!stoppingToken.IsCancellationRequested);
    }

    private Task CancelSubscriptionsAsync(
        IEnumerable<(string hostUrl, CancellationTokenSource cancellationSource)> staleHosts)
    {
        var cancellationTasks = staleHosts.Select(entry =>
        {
            var (url, cancellationSource) = entry;
            logger.LogInformation("[HOST-POLLING][{Host}] Host no longer registered in the database. Cancelling.",
                url);
            return cancellationSource.CancelAsync();
        });

        return Task.WhenAll(cancellationTasks);
    }
}