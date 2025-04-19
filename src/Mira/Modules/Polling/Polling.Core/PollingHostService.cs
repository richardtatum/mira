using Microsoft.Extensions.Hosting;

namespace Polling.Core;

public class PollingHostService(IHostPollingService hostPollingService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await hostPollingService.StartPollingAsync(stoppingToken);
    }
}