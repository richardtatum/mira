namespace Shared.Core.Interfaces;

public interface IChangeTrackingService
{
    Task ExecuteAsync(string hostUrl);
}