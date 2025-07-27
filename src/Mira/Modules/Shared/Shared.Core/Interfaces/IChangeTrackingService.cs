using Shared.Core.Models;

namespace Shared.Core.Interfaces;

public interface IChangeTrackingService
{
    Task ExecuteAsync(Host host);
}