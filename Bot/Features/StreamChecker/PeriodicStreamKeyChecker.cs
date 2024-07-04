using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mira.Features.StreamChecker.Repositories;

namespace Mira.Features.StreamChecker;

public class PeriodicStreamKeyChecker : BackgroundService
{
    private readonly QueryRepository _query;
    private readonly StreamNotificationService _service;
    private readonly ILogger<PeriodicStreamKeyChecker> _logger;

    public PeriodicStreamKeyChecker(QueryRepository query, StreamNotificationService service, ILogger<PeriodicStreamKeyChecker> logger)
    {
        _query = query;
        _service = service;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        // Need to consider the following:
        // - Only messaging once for live, not every 30s (add notification status table, links to notif, adds message link so it can be edited?)
        // - Grouping pings together by host, then one ping per channel, per streamkey
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var hosts = await _query.GetHostsAsync();
            if (hosts.Length == 0)
            {
                _logger.LogInformation("[PERIODIC-CHECKER] No hosts found. Skipping;");
                continue;
            }
            
            foreach (var host in hosts)
            {
                _logger.LogInformation("[PERIODIC-CHECKER] Checking notification requests for host {Host}", host.Url);
                var notifications = await _query.GetNotificationsAsync(host.Id);
                _logger.LogInformation("[PERIODIC-CHECKER] {Count} Notification(s) found.", notifications.Length);
                await _service.CheckStreamsAsync(host, notifications);
            }
        }
    }
}