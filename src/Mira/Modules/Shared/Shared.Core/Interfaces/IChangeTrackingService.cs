namespace Shared.Core.Interfaces;

public interface IChangeTrackingService
{
    public Task ExecuteAsync(string hostUrl);
}