
using Polling.Core.Models;

namespace Polling.Core;

public interface ISubscriptionManager
{
    bool IsSubscribed(string hostUrl);
    void Subscribe(Host host, CancellationToken baseCancellationToken);
    void CleanupSubscriptions(IEnumerable<string> activeHostUrls);
}