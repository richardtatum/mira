using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polling.Core.Options;
using Polling.Core.Repositories;

namespace Polling.Core;

public class HostPollingService(
    QueryRepository queryRepository,
    ILogger<HostPollingService> logger,
    ISubscriptionManager subscriptionManager,
    IOptions<PollingOptions> options)
    : IHostPollingService
{
    private readonly PollingOptions _options = options.Value;


    public async Task StartPollingAsync(CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.NewHostIntervalSeconds));
        do
        {
            var hosts = await queryRepository.GetHostsAsync();
            foreach (var host in hosts)
            {
                if (subscriptionManager.IsSubscribed(host.Url))
                {
                    continue;
                }
                
                logger.LogInformation(
                    "[HOST-POLLING] New host found: {Host}. Creating subscription. Interval: {Seconds}s", host.Url,
                    host.PollIntervalSeconds);
                
                subscriptionManager.Subscribe(host, cancellationToken);
            }

            var activeHosts = hosts.Select(host => host.Url);
            subscriptionManager.CleanupSubscriptions(activeHosts);

            await timer.WaitForNextTickAsync(cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
    }
}