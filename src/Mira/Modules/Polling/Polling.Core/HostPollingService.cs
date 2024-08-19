using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polling.Core.Options;
using Polling.Core.Repositories;

namespace Polling.Core;

public class HostPollingService : IHostPollingService
{
    private readonly QueryRepository _queryRepository;
    private readonly ILogger<HostPollingService> _logger;
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly PollingOptions _options;


    public HostPollingService(QueryRepository queryRepository, ILogger<HostPollingService> logger, ISubscriptionManager subscriptionManager, IOptions<PollingOptions> options)
    {
        _queryRepository = queryRepository;
        _logger = logger;
        _subscriptionManager = subscriptionManager;
        _options = options.Value;
    }

    public async Task StartPollingAsync(CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.NewHostIntervalSeconds));
        do
        {
            var hosts = await _queryRepository.GetHostsAsync();
            foreach (var host in hosts)
            {
                if (_subscriptionManager.IsSubscribed(host.Url))
                {
                    continue;
                }
                
                _logger.LogInformation(
                    "[HOST-POLLING] New host found: {Host}. Creating subscription. Interval: {Seconds}s", host.Url,
                    host.PollIntervalSeconds);
                
                _subscriptionManager.Subscribe(host, cancellationToken);
            }

            var activeHosts = hosts.Select(host => host.Url);
            _subscriptionManager.CleanupSubscriptions(activeHosts);

            await timer.WaitForNextTickAsync(cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
    }
}