using ChangeTracking.Core.Models;
using Shared.Core;
using Stream = ChangeTracking.Core.Models.Stream;

namespace ChangeTracking.Tests;

public class StreamTests
{
    [Fact]
    public async Task OnSendNewMessage_ShouldFire_WhenStreamIsStarting()
    {
        var hostUrl = "exampleHostUrl";
        var eventRaised = false;
        ulong? channelId = null;
        int? viewerCount = null;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, null, currentStream);

        stream.OnSendNewMessage += (eventChannelId, _, _, eventViewers, _, _) =>
        {
            eventRaised = true;
            channelId = eventChannelId;
            viewerCount = eventViewers;
            return Task.FromResult((ulong?)12345);
        };

        await stream.FireEventsAsync();
        
        Assert.True(eventRaised);
        Assert.Equal(subscription.ChannelId, channelId);
        Assert.Equal(currentStream.ViewerCount, viewerCount);
    }
    
    [Fact]
    public async Task OnSendNewMessage_ShouldNotFire_WhenStreamIsLive()
    {
        var hostUrl = "exampleHostUrl";
        var viewers = 150;
        var eventRaised = false;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Existing stream is live
        var existingStream = new StreamRecord
        {
            MessageId = 12345,
            Status = StreamStatus.Live
        };
        
        // Current stream is live
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ],
            WhepSessions = Enumerable.Range(1, viewers).Select(_ => new Viewer()).ToArray()
        };
        
        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendNewMessage += (_, _, _, _, _, _) =>
        {
            eventRaised = true;
            return Task.FromResult((ulong?)12345);
        };

        await stream.FireEventsAsync();
        
        Assert.False(eventRaised);
    }

    [Fact]
    public async Task OnSendUpdateMessage_ShouldFire_WhenStreamIsLive()
    {
        var hostUrl = "exampleHostUrl";
        var eventRaised = false;
        int? viewerCount = null;
        ulong? messageId = null;
        StreamStatus? status = null;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Existing stream is live
        var existingStream = new StreamRecord
        {
            MessageId = 12345,
            Status = StreamStatus.Live
        };
        
        // Current stream is live
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ],
            WhepSessions = Enumerable.Range(1, 150).Select(_ => new Viewer()).ToArray()
        };
        
        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendUpdateMessage += (eventMessageId, _, eventStatus, _, eventViewerCount, _, _) =>
        {
            eventRaised = true;
            messageId = eventMessageId;
            status = eventStatus;
            viewerCount = eventViewerCount;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();
        
        Assert.True(eventRaised);
        Assert.Equal(existingStream.MessageId, messageId);
        Assert.Equal(StreamStatus.Live, status);
        Assert.Equal(currentStream.ViewerCount, viewerCount);
    }
    
    [Fact]
    public async Task OnSendUpdateMessage_ShouldFire_WhenStreamIsEnding()
    {
        var hostUrl = "exampleHostUrl";
        var existingViewers = 150;
        var eventRaised = false;
        StreamStatus? status = null;
        ulong? messageId = null;
        
        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Existing stream is live
        var existingStream = new StreamRecord
        {
            MessageId = 12345,
            Status = StreamStatus.Live,
            ViewerCount = existingViewers
        };

        // Current stream is offline
        var currentStream = new KeySummary();

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendUpdateMessage += (eventMessageId, channelId, eventStatus, url, count, duration, playing) =>
        {
            eventRaised = true;
            messageId = eventMessageId;
            status = eventStatus;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.True(eventRaised);
        Assert.Equal(existingStream.MessageId, messageId);
        Assert.Equal(StreamStatus.Offline, status);
    }
    
    [Fact]
    public async Task OnSendUpdateMessage_ShouldNotFire_WhenStreamIsOffline()
    {
        var hostUrl = "exampleHostUrl";
        var eventRaised = false;
        
        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Existing stream is offline
        var existingStream = new StreamRecord
        {
            MessageId = 12345,
            Status = StreamStatus.Offline
        };
        
        // Current stream is offline
        var currentStream = new KeySummary();

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendUpdateMessage += (_, _, _, _, _, _, _) =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();
        
        Assert.False(eventRaised);
    }
    
    [Fact]
    public async Task OnSendUpdateMessage_ShouldNotFire_WhenNoExistingStream()
    {
        var hostUrl = "exampleHostUrl";
        var eventRaised = false;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        // No existing stream
        StreamRecord? existingStream = null;

        // Current stream is offline
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendUpdateMessage += (_, _, _, _, _, _, _) =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.False(eventRaised);
    }
    
    [Fact]
    public async Task OnRecordStateChange_ShouldFire_WhenStreamStatusChangesAndMessageSent()
    {
        var hostUrl = "exampleHostUrl";
        var messageId = 12345UL;
        var eventRaised = false;
        ulong? recordMessageId = null;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        // Previously offline
        StreamRecord? existingStream = null;

        // Current stream online
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendNewMessage += (_, _, _, _, _, _) => Task.FromResult((ulong?)messageId);
        
        stream.OnRecordStateChange += record =>
        {
            recordMessageId = record.MessageId;
            eventRaised = true;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.True(eventRaised);
        Assert.Equal(messageId, recordMessageId);
    }
    
    [Fact]
    public async Task OnRecordStateChange_ShouldNotFire_WhenStreamStatusChangesAndMessageNotSent()
    {
        var hostUrl = "exampleHostUrl";
        var messageId = 12345UL;
        var eventRaised = false;
        ulong? recordMessageId = null;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        // Previously offline
        StreamRecord? existingStream = null;

        // Current stream online
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        
        // No .OnSendNewMessage event registered, so state change should not be recorded
        stream.OnRecordStateChange += _ =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.False(eventRaised);
    }
    
    [Fact]
    public async Task OnRecordStateChange_ShouldNotFire_WhenStreamStatusHasNotChanged()
    {
        var hostUrl = "exampleHostUrl";
        var messageId = 12345UL;
        var eventRaised = false;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        // Existing stream is live
        var existingStream = new StreamRecord
        {
            MessageId = 12345,
            Status = StreamStatus.Live
        };

        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnRecordStateChange += _ =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.False(eventRaised);
    }
    
    [Fact]
    public async Task OnRecordStateChange_ShouldUpdateExistingStartTimeEndTime_WhenNewStreamIsStarting()
    {
        var hostUrl = "exampleHostUrl";
        var existingStartTime = new DateTime(2020, 03, 11);
        var existingEndTime = new DateTime(2020, 03, 12);
        var eventRaised = false;
        (DateTime startTime, DateTime? endTime) result = (default, default);

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Old offline record
        var existingStream = new StreamRecord
        {
            Id = 1,
            MessageId = 12345,
            SubscriptionId = 1,
            StartTime = existingStartTime,
            EndTime = existingEndTime,
            Status = StreamStatus.Offline
        };

        // New stream
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendNewMessage += (_, _, _, _, _, _) => Task.FromResult((ulong?)54321);
        stream.OnRecordStateChange += record =>
        {
            eventRaised = true;
            result.startTime = record.StartTime;
            result.endTime = record.EndTime;
            
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.True(eventRaised);
        Assert.NotEqual(existingStartTime, result.startTime);
        Assert.NotEqual(existingEndTime, result.endTime);
        Assert.Null(result.endTime);
    }
    
    [Fact]
    public async Task OnRecordStateChange_ShouldSetNewMessageId_WhenNewStreamIsStarting()
    {
        var hostUrl = "exampleHostUrl";
        var newMessageId = 54321UL;
        var eventRaised = false;
        ulong? recordedMessageId = null;
        
        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Old offline record
        var existingStream = new StreamRecord
        {
            Id = 1,
            MessageId = 12345,
            SubscriptionId = 1,
            StartTime = new DateTime(2020, 03, 11),
            EndTime = new DateTime(2020, 03, 12),
            Status = StreamStatus.Offline
        };

        // New live stream
        var currentStream = new KeySummary
        {
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendNewMessage += (_, _, _, _, _, _) => Task.FromResult((ulong?)54321);
        stream.OnRecordStateChange += record =>
        {
            eventRaised = true;
            recordedMessageId = record.MessageId;
            
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.True(eventRaised);
        Assert.Equal(newMessageId, recordedMessageId);
    }

    [Fact]
    public async Task ToStreamRecord_ShouldUseExistingMessageId_WhenStreamIsUpdating()
    {
        var hostUrl = "exampleHostUrl";
        var eventRaised = false;
        ulong? recordedMessageId = null;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Existing live stream
        var existingStream = new StreamRecord
        {
            Id = 1,
            MessageId = 12345,
            SubscriptionId = 1,
            StartTime = new DateTime(2020, 03, 11),
            EndTime = null,
            Status = StreamStatus.Live
        };

        // Current offline stream
        var currentStream = new KeySummary();

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendUpdateMessage += (id, channelId, status, url, count, duration, playing) => Task.CompletedTask;
        stream.OnRecordStateChange += record =>
        {
            eventRaised = true;
            recordedMessageId = record.MessageId;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();
            
        Assert.Equal(existingStream.MessageId, recordedMessageId);
    }
    
    [Fact]
    public async Task ToStreamRecord_ShouldMarkCurrentStreamOffline_WhenTimeSinceLastFrameOutOfWindow()
    {
        var hostUrl = "exampleHostUrl";
        var secondsSinceLastFrame = 30;
        var eventRaised = false;
        StreamStatus? recordedStreamStatus = null;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        // Old live record
        var existingStream = new StreamRecord
        {
            Id = 1,
            MessageId = 12345,
            SubscriptionId = 1,
            StartTime = DateTime.UtcNow,
            Status = StreamStatus.Live
        };

        // New stream 
        var currentStream = new KeySummary
        {
            VideoStreams = [
                new VideoStream
                {
                    // Seconds since last frame is larger than the allowed amount, making it offline
                    LastKeyFrameSeen = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(secondsSinceLastFrame))
                }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.OnSendUpdateMessage += (id, channelId, status, url, count, duration, playing) => Task.CompletedTask;
        stream.OnRecordStateChange += record =>
        {
            eventRaised = true;
            recordedStreamStatus = record.Status;
            return Task.CompletedTask;
        };

        await stream.FireEventsAsync();

        Assert.True(eventRaised);
        Assert.Equal(StreamStatus.Offline, recordedStreamStatus);
    }
}