namespace Polling.Core;

public interface IHostPollingService
{
    Task StartPollingAsync(CancellationToken cancellationToken = default);
}