using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mira.Features.StreamChecker.Repositories;
using Host = Mira.Features.StreamChecker.Models.Host;

namespace Mira.Features.StreamChecker;

public class PeriodicStreamChecker(
    QueryRepository query,
    StreamNotificationService service,
    ILogger<PeriodicStreamChecker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        // TODO: Consider the fact that removed hosts could be replaced by a different host with the same Id, causing problems with running subscriptions
        var subscribedHosts = new Dictionary<int, (CancellationTokenSource cancellationTokenSource, string url)>();
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation("[HOST-CHECKER] Checking for new hosts.");
            var hosts = await query.GetHostsAsync();
            foreach (var host in hosts)
            {
                if (subscribedHosts.ContainsKey(host.Id))
                {
                    continue;
                }

                logger.LogInformation(
                    "[HOST-CHECKER] New host found: {Host}. Creating subscription. Interval: {Seconds}s", host.Url,
                    host.PollIntervalSeconds);

                // Create a child of the provided cancellation token
                var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                subscribedHosts[host.Id] = (cancellationSource, host.Url);
                _ = SubscribeToHostAsync(TimeSpan.FromSeconds(host.PollIntervalSeconds), host,
                    cancellationSource.Token);
            }

            // These are hosts that were previously subscribed but have since been removed from the DB
            var staleHosts = subscribedHosts
                .Where(subscription =>
                    !hosts.Select(host => host.Id).Contains(subscription.Key)
                )
                .Select(entry => entry.Value)
                .ToArray();

            await CancelSubscriptionsAsync(staleHosts);
        }
    }

    private async Task SubscribeToHostAsync(TimeSpan period, Host host, CancellationToken stoppingToken)
    {
        // Shortest interval period is 30 seconds
        period = period.TotalSeconds < 30 ? TimeSpan.FromSeconds(30) : period;

        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var subscriptions = await query.GetSubscriptionsAsync(host.Id);
            if (subscriptions.Length == 0)
            {
                logger.LogInformation("[KEY-CHECKER][{Host}] No key subscriptions found. Skipping.", host.Url);
                continue;
            }

            logger.LogInformation(
                "[KEY-CHECKER][{Host}] {Subscriptions} key subscription(s) found. Checking for stream updates.",
                host.Url, subscriptions.Length);
            await service.CheckStreamsAsync(host, subscriptions);
        }
    }

    private Task CancelSubscriptionsAsync(
        IEnumerable<(CancellationTokenSource cancellationSource, string hostUrl)> staleHosts)
    {
        var cancellationTasks = staleHosts.Select(entry =>
        {
            var (cancellationSource, url) = entry;
            logger.LogInformation("[HOST-CHECKER][{Host}] Host no longer registered in the database. Cancelling.",
                url);
            return cancellationSource.CancelAsync();
        });

        return Task.WhenAll(cancellationTasks);
    }
}