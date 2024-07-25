using System.Text.Json;
using ChangeTracking.Core.Extensions;
using Shared.Core;

namespace ChangeTracking.Core.Models;

internal class Stream(string hostUrl)
{
    // Base
    private string HostUrl { get; } = hostUrl;

    // StreamRecord
    private int? RecordId { get; set; }
    private StreamStatus? RecordedStatus { get; set; }
    private ulong? RecordedMessageId { get; set; }
    private DateTime? RecordedStartTime { get; set; }
    private DateTime? RecordedEndTime { get; set; }
    private bool ExistingStreamLoaded { get; set; }

    // Live Stream
    private int? CurrentViewerCount { get; set; }
    private DateTime? CurrentStartTime { get; set; }
    private bool CurrentStreamLoaded { get; set; }

    // Subscription
    private int SubscriptionId { get; set; }
    private string StreamKey { get; set; } = null!;
    private ulong ChannelId { get; set; }
    private bool SubscriptionLoaded { get; set; }

    // Messaging
    private ulong? MessageId { get; set; }
    private bool MessageSent { get; set; }

    // Meta
    public StreamStatus Status => DetailedStreamStatus.ToStreamStatus();
    public DateTime StartTime => DetailedStreamStatus switch
    {
        DetailedStreamStatus.Starting => CurrentStartTime ?? DateTime.UtcNow,
        _ => RecordedStartTime ?? DateTime.UtcNow
    };
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow).Subtract(StartTime);
    public DateTime? EndTime => DetailedStreamStatus switch
    {
        DetailedStreamStatus.Starting => null,
        DetailedStreamStatus.Live => null,
        DetailedStreamStatus.Ending => DateTime.UtcNow,
        DetailedStreamStatus.Offline => RecordedEndTime,
        _ => throw new ArgumentOutOfRangeException()
    };


    // Internal meta
    private bool IsValid => SubscriptionLoaded && ExistingStreamLoaded && CurrentStreamLoaded;
    private DetailedStreamStatus DetailedStreamStatus { get; set; }

    // Offline is the only status we ignore
    public bool StreamUpdated => IsValid && DetailedStreamStatus != DetailedStreamStatus.Offline;
    public bool SendNewMessage => IsValid && DetailedStreamStatus == DetailedStreamStatus.Starting;
    public bool SendUpdateMessage => IsValid && DetailedStreamStatus == DetailedStreamStatus.Live ||
                                     DetailedStreamStatus == DetailedStreamStatus.Ending;

    public Stream LoadExistingStreamData(StreamRecord? record)
    {
        // It's still valid if this data doesn't exist. First ever stream would be without it
        RecordId = record?.Id;
        RecordedStatus = record?.Status;
        RecordedMessageId = record?.MessageId;
        RecordedStartTime = record?.StartTime;
        RecordedEndTime = record?.EndTime;

        ExistingStreamLoaded = true;
        return this;
    }

    public Stream LoadCurrentStreamData(KeySummary? currentStream)
    {
        if (currentStream is null)
        {
            DetailedStreamStatus = RecordedStatus.ToDetailedStreamStatus(streamCurrentlyLive: false);
            CurrentStreamLoaded = true;
            return this;
        }

        DetailedStreamStatus = RecordedStatus.ToDetailedStreamStatus(currentStream.IsLive);
        CurrentViewerCount = currentStream.ViewerCount;
        CurrentStartTime = currentStream.StartTime;
        CurrentStreamLoaded = true;
        return this;
    }

    public Stream LoadSubscriptionData(Subscription subscription)
    {
        SubscriptionId = subscription.Id;
        StreamKey = subscription.StreamKey;
        ChannelId = subscription.ChannelId;

        SubscriptionLoaded = true;
        return this;
    }

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

    public (ulong messageId, ulong channelId, StreamStatus status, string url, int viewerCount, TimeSpan duration)
        DeconstructIntoUpdateMessage()
    {
        if (!SendUpdateMessage)
        {
            throw new InvalidOperationException($"Stream not in valid state to deconstruct into a message update format. Stream: {JsonSerializer.Serialize(this)}." );
        }

        var url = $"{HostUrl}/{StreamKey}";
        var duration = (RecordedEndTime ?? DateTime.UtcNow).Subtract(StartTime);
        return (RecordedMessageId!.Value, ChannelId, Status, url, CurrentViewerCount ?? 0, duration);
    }

    // Update this to return with Result<StreamRecord> type?
    public StreamRecord ToStreamRecord()
    {
        if (RecordedMessageId is null && MessageId is null)
        {
            throw new InvalidOperationException(
                "Can't generate a stream record for a message that has never been sent.");
        }

        // We prioritise any new messageIds first
        var messageId = (MessageId ?? RecordedMessageId)!.Value;

        return new StreamRecord
        {
            Id = RecordId ?? null,
            SubscriptionId = SubscriptionId,
            Status = DetailedStreamStatus.ToStreamStatus(),
            ViewerCount = CurrentViewerCount ?? 0,
            MessageId = messageId,
            StartTime = StartTime,
            EndTime = EndTime
        };
    }
}