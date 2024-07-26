using System.Text.Json;
using ChangeTracking.Core.Extensions;
using Shared.Core;

namespace ChangeTracking.Core.Models;

internal class Stream
{
    public Stream(string hostUrl, Subscription subscription, StreamRecord? existingStream, KeySummary? currentStream)
    {
        HostUrl = hostUrl;
        
        // Subscription
        SubscriptionId = subscription.Id;
        StreamKey = subscription.StreamKey;
        ChannelId = subscription.ChannelId;
        
        // Existing Stream. First stream would have no record, so being null is valid
        ExistingStreamId = existingStream?.Id;
        ExistingStreamStatus = existingStream?.Status;
        ExistingStreamMessageId = existingStream?.MessageId;
        ExistingStreamStartTime = existingStream?.StartTime;
        ExistingStreamEndTime = existingStream?.EndTime;
        Playing = existingStream?.Playing;
        
        // Current Stream
        DetailedStreamStatus = ExistingStreamStatus.ToDetailedStreamStatus(currentStream?.IsLive ?? false);
        if (currentStream is not null)
        {
            CurrentViewerCount = currentStream.ViewerCount;
            CurrentStartTime = currentStream.StartTime;
        }
    }

    private string HostUrl { get; }
    private int SubscriptionId { get; }
    private string StreamKey { get; }
    private ulong ChannelId { get; }
    private int? ExistingStreamId { get; }
    private StreamStatus? ExistingStreamStatus { get; }
    private ulong? ExistingStreamMessageId { get; }
    private DateTime? ExistingStreamStartTime { get; }
    private DateTime? ExistingStreamEndTime { get; }
    private string? Playing { get; set; }
    private int? CurrentViewerCount { get; }
    private DateTime? CurrentStartTime { get; }
    

    // Messaging
    private ulong? MessageId { get; set; }
    private bool MessageSent { get; set; }

    // Simplified database stream status. The db only cares about online/offline. Further granularity is to allow for
    // easy state management in this class (i.e. determine when to send a new message vs update)
    private StreamStatus Status => DetailedStreamStatus.ToStreamStatus();
    private DateTime StartTime => DetailedStreamStatus switch
    {
        DetailedStreamStatus.Starting => CurrentStartTime ?? DateTime.UtcNow,
        _ => ExistingStreamStartTime ?? DateTime.UtcNow
    };
    private TimeSpan Duration => (EndTime ?? DateTime.UtcNow).Subtract(StartTime);
    private DateTime? EndTime => DetailedStreamStatus switch
    {
        DetailedStreamStatus.Starting => null,
        DetailedStreamStatus.Live => null,
        DetailedStreamStatus.Ending => DateTime.UtcNow,
        DetailedStreamStatus.Offline => ExistingStreamEndTime,
        _ => throw new ArgumentOutOfRangeException()
    };

    // Internal meta
    private DetailedStreamStatus DetailedStreamStatus { get; set; }

    // Offline is the only status we ignore
    public bool StreamUpdated => DetailedStreamStatus != DetailedStreamStatus.Offline;
    // We only send a new message when a stream is starting
    public bool SendNewMessage => DetailedStreamStatus == DetailedStreamStatus.Starting;
    public bool SendUpdateMessage => ExistingStreamMessageId is not null && DetailedStreamStatus == DetailedStreamStatus.Live ||
                                     DetailedStreamStatus == DetailedStreamStatus.Ending;
    

    public void MarkMessageSent(ulong messageId)
    {
        MessageId = messageId;
        MessageSent = true;
    }

    public (ulong channelId, StreamStatus status, string url, int viewerCount, TimeSpan duration)
        DeconstructIntoNewMessage()
    {
        if (!SendNewMessage)
        {
            throw new InvalidOperationException($"Stream not in valid state to deconstruct into new message format. Stream: {JsonSerializer.Serialize(this)}." );
        }

        var url = $"{HostUrl}/{StreamKey}";
        return (ChannelId, Status, url, CurrentViewerCount ?? 0, Duration);
    }

    public (ulong messageId, ulong channelId, StreamStatus status, string url, int viewerCount, TimeSpan duration, string? playing)
        DeconstructIntoUpdateMessage()
    {
        if (!SendUpdateMessage)
        {
            throw new InvalidOperationException($"Stream not in valid state to deconstruct into a message update format. Stream: {JsonSerializer.Serialize(this)}." );
        }

        var url = $"{HostUrl}/{StreamKey}";
        return (ExistingStreamMessageId!.Value, ChannelId, Status, url, CurrentViewerCount ?? 0, Duration, Playing);
    }

    // Update this to return with Result<StreamRecord> type?
    public StreamRecord ToStreamRecord()
    {
        if (ExistingStreamMessageId is null && MessageId is null)
        {
            throw new InvalidOperationException(
                "Can't generate a stream record for a message that has never been sent.");
        }

        // We prioritise any new messageIds first
        var messageId = (MessageId ?? ExistingStreamMessageId)!.Value;

        // Ensure all new streams start with an empty playing
        var playing = SendNewMessage ? null : Playing;

        return new StreamRecord
        {
            Id = ExistingStreamId ?? null,
            SubscriptionId = SubscriptionId,
            Status = DetailedStreamStatus.ToStreamStatus(),
            ViewerCount = CurrentViewerCount ?? 0,
            MessageId = messageId,
            StartTime = StartTime,
            EndTime = EndTime,
            Playing = playing
        };
    }
}