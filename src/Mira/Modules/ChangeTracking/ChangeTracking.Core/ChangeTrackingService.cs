using ChangeTracking.Core.Models;
using ChangeTracking.Core.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Core.Interfaces;
using Shared.Core.Models;
using Stream = ChangeTracking.Core.Models.Stream;

namespace ChangeTracking.Core;

internal class ChangeTrackingService(
    BroadcastBoxClient client,
    ILogger<ChangeTrackingService> logger,
    QueryRepository query,
    CommandRepository command,
    IMessageService messageService) : IChangeTrackingService
{
    private readonly DetailedStreamStatus[] _logStreamStatuses =
    [
        DetailedStreamStatus.Starting,
        DetailedStreamStatus.Ending
    ];

    public async Task ExecuteAsync(Host host)
    {
        var subscriptions = await query.GetSubscriptionsAsync(host.Url);
        if (subscriptions.Length == 0)
        {
            logger.LogInformation("[CHANGE-TRACKING][{HostUrl}] No key subscriptions found. Skipping.", host.Url);
            return;
        }

        var statusChanges = new Dictionary<string, DetailedStreamStatus>();
        var currentStreams = await client.GetStreamsAsync(host);
        var existingStreams = await query.GetStreamsAsync(subscriptions.Select(x => x.Id));

        var streams = subscriptions
            .Select(async subscription =>
            {
                // Create the stream object which manages the state and any changes
                var existingStream = existingStreams.FirstOrDefault(x => x.SubscriptionId == subscription.Id);
                var currentStream = currentStreams.FirstOrDefault(x => x.StreamKey == subscription.StreamKey);
                var stream = new Stream(host.Url, subscription, existingStream, currentStream);
                
                // Register events
                stream.OnSendNewMessage += (channelId, status, url, viewerCount, duration, playing) =>
                {
                    RecordStatusChange(subscription.StreamKey, stream.DetailedStreamStatus);
                    return messageService.SendAsync(channelId, status, url, viewerCount, duration, playing);
                };

                stream.OnSendUpdateMessage += (id, channelId, status, s, i, span, playing) =>
                {
                    RecordStatusChange(subscription.StreamKey, stream.DetailedStreamStatus);
                    return messageService.ModifyAsync(id, channelId, status, s, i, span, playing);
                };
                    
                stream.OnRecordStateChange += command.UpsertStreamRecord;

                // This is a terrible pattern and needs redoing
                await stream.FireEventsAsync();
            })
            .ToArray();
        
        LogStatusChanges(host.Url, statusChanges);

        await Task.WhenAll(streams);
        return;

        void RecordStatusChange(string streamKey, DetailedStreamStatus status) => statusChanges.TryAdd(streamKey, status);
    }

    private void LogStatusChanges(string hostUrl, Dictionary<String, DetailedStreamStatus> statusChanges)
    {
        foreach (var (streamKey, status) in statusChanges.Where(kv => _logStreamStatuses.Contains(kv.Value)))
        {
            logger.LogInformation("[CHANGE-TRACKING][{Host}] '{Subscription}' stream is now {Status}", hostUrl, streamKey, status);
        }
    }
}