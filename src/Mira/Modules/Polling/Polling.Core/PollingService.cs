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

        var subscribedHosts = new SubscriptionTracker(logger);
        do
        {
            logger.LogInformation("[HOST-POLLING] Checking for new hosts...");
            var hosts = await query.GetHostsAsync();
            
            foreach (var host in hosts)
            {
                if (subscribedHosts.Contains(host.Url))
                {
                    continue;
                }

                logger.LogInformation(
                    "[HOST-POLLING] New host found: {Host}. Creating subscription. Interval: {Seconds}s", host.Url,
                    host.PollIntervalSeconds);

                var cancellationToken = subscribedHosts.Register(host.Url, stoppingToken);
                _ = SubscribeToHostAsync(host, cancellationToken);
            }

            var activeHostUrls = hosts.Select(host => host.Url);
            await subscribedHosts.Cleanup(activeHostUrls);
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
}