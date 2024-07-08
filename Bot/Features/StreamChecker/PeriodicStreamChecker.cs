using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mira.Features.StreamChecker.Repositories;
using Host = Mira.Features.Shared.Models.Host;

namespace Mira.Features.StreamChecker;

public class PeriodicStreamChecker : BackgroundService
{
    private readonly QueryRepository _query;
    private readonly StreamNotificationService _service;
    private readonly ILogger<PeriodicStreamChecker> _logger;

    public PeriodicStreamChecker(QueryRepository query, StreamNotificationService service,
        ILogger<PeriodicStreamChecker> logger)
    {
        _query = query;
        _service = service;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        var subscribedHosts = new Dictionary<int, CancellationTokenSource>();
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // TODO: Check for hosts that are subscribed, but no longer in the DB to clear them
            _logger.LogInformation("[HOST-CHECKER] Checking for hosts.");
            var hosts = await _query.GetHostsAsync();
            foreach (var host in hosts)
            {
                if (subscribedHosts.ContainsKey(host.Id))
                {
                    continue;
                }

                _logger.LogInformation(
                    "[HOST-CHECKER] New host found: {Host}. Creating subscription. Interval: {Seconds}s", host.Url,
                    host.PollIntervalSeconds);

                var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                subscribedHosts[host.Id] = cancellationSource;
                _ = SubscribeToHostAsync(TimeSpan.FromSeconds(host.PollIntervalSeconds), host, cancellationSource.Token);
            }
        }
    }

    private async Task SubscribeToHostAsync(TimeSpan period, Host host, CancellationToken stoppingToken)
    {
        // Shortest interval period is 30 seconds
        period = period.TotalSeconds < 30 ? TimeSpan.FromSeconds(30) : period;

        using var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var subscriptions = await _query.GetSubscriptionsAsync(host.Id);
            if (subscriptions.Length == 0)
            {
                _logger.LogInformation("[KEY-CHECKER][{HOST}] No key subscriptions found. Skipping.", host.Url);
                return;
            }

            _logger.LogInformation(
                "[KEY-CHECKER][{HOST}] {Subscriptions} key subscription(s) found. Checking for stream updates.",
                host.Url, subscriptions.Length);
            await _service.CheckStreamsAsync(host, subscriptions);
        }
    }
}