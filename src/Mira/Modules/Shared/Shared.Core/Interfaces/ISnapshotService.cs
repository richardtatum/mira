namespace Shared.Core.Interfaces;

public interface ISnapshotService
{
    Task ExecuteAsync(string hostUrl, CancellationToken cancellationToken = default);
}