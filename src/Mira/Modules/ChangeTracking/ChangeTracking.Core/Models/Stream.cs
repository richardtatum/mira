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

    public event SendNewMessageHandler? OnSendNewMessage;
    public event SendUpdatedMessageHandler? OnSendUpdateMessage;
    public event Func<StreamRecord, Task>? OnRecordStateChange; 
    public delegate Task<ulong?> SendNewMessageHandler(ulong channelId, StreamStatus status, string url, int viewerCount, TimeSpan duration, string? playing = null);
    public delegate Task SendUpdatedMessageHandler(ulong messageId, ulong channelId, StreamStatus status, string url, int viewerCount, TimeSpan duration, string? playing = null);
    
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
    // private bool MessageSent { get; set; }

    // Simplified database stream status. The db only cares about online/offline. Further granularity (DetailedStreamStatus)
    // is to allow for easy state management in this class (i.e. determine when to send a new message vs update)
    private StreamStatus Status => DetailedStreamStatus.ToStreamStatus();
    private DateTime StartTime => DetailedStreamStatus switch
    {
        DetailedStreamStatus.Starting => CurrentStartTime ?? DateTime.UtcNow,
        _ => ExistingStreamStartTime ?? DateTime.UtcNow
    };

    private string Url => $"{HostUrl}/{StreamKey}";
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
    
    // Only send a new message when a stream is starting and a message hasn't already been sent
    public bool SendNewMessage => DetailedStreamStatus == DetailedStreamStatus.Starting && 
                                  MessageId is null;
    public bool SendUpdateMessage => DetailedStreamStatus == DetailedStreamStatus.Live ||
                                     DetailedStreamStatus == DetailedStreamStatus.Ending &&
                                     ExistingStreamMessageId is not null;
    
    // We ignore offline and don't make updates unless there is a status change and a message Id present
    private bool RecordStateChange => DetailedStreamStatus != DetailedStreamStatus.Offline &&
                                      ExistingStreamStatus != Status &&
                                      (ExistingStreamMessageId is not null || MessageId is not null);

    // Update this to return with Result<StreamRecord> type?
    private StreamRecord ToStreamRecord()
    {
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

    public async Task FireEventsAsync()
    {
        if (SendNewMessage && OnSendNewMessage is not null)
        {
            // Need to await this, which means it needs to be in an answer func
            MessageId = await OnSendNewMessage.Invoke(ChannelId, Status, Url, CurrentViewerCount ?? 0, Duration);
        }

        if (SendUpdateMessage && OnSendUpdateMessage is not null)
        {
            await OnSendUpdateMessage.Invoke(ExistingStreamMessageId!.Value, ChannelId, Status, Url, CurrentViewerCount ?? 0, Duration, Playing);
        }

        if (RecordStateChange && OnRecordStateChange is not null)
        {
            var record = ToStreamRecord();
            await OnRecordStateChange.Invoke(record);
        }
    }
}