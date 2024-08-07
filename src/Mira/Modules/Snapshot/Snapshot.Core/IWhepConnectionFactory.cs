namespace Snapshot.Core;

public interface IWhepConnectionFactory
{
    Task<WhepConnection?> CreateAsync(string hostUrl, string streamKey);
    Task<WhepConnection?> CreateAsync(string hostUrl, string streamKey, CancellationToken cancellationToken);
}