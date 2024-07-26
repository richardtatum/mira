using ChangeTracking.Core.Models;
using Shared.Core;
using Stream = ChangeTracking.Core.Models.Stream;

namespace ChangeTracking.Tests;

public class StreamTests
{
    [Fact]
    public void DeconstructIntoNewMessage_ShouldReturnCorrectTuple_WhenStreamIsStarting()
    {
        var hostUrl = "exampleHostUrl";

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
        var result = stream.DeconstructIntoNewMessage();

        Assert.Equal(subscription.ChannelId, result.channelId);
        Assert.Equal(currentStream.ViewerCount, result.viewerCount);
    }
    
    [Fact]
    public void DeconstructIntoNewMessage_ShouldThrowException_WhenStreamIsNotStarting()
    {
        var hostUrl = "exampleHostUrl";

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        var existingStream = new StreamRecord
        {
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

        Assert.Throws<InvalidOperationException>(() => stream.DeconstructIntoNewMessage());
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldReturnCorrectTuple_WhenStreamIsLive()
    {
        var hostUrl = "exampleHostUrl";
        var viewers = 150;

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
        var result = stream.DeconstructIntoUpdateMessage();

        Assert.Equal(existingStream.MessageId, result.messageId);
        Assert.Equal(viewers, result.viewerCount);
        Assert.Equal(StreamStatus.Live, result.status);
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldReturnCorrectTuple_WhenStreamIsEnding()
    {
        var hostUrl = "exampleHostUrl";
        var existingViewers = 150;
        
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
        var result = stream.DeconstructIntoUpdateMessage();

        Assert.Equal(existingStream.MessageId, result.messageId);
        Assert.Equal(StreamStatus.Offline, result.status);
        Assert.NotEqual(existingViewers, result.viewerCount);
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldThrowException_WhenStreamIsOffline()
    {
        var hostUrl = "exampleHostUrl";
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
        
        Assert.Throws<InvalidOperationException>(() => stream.DeconstructIntoUpdateMessage());
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldThrowException_WhenNoExistingStream()
    {
        var hostUrl = "exampleHostUrl";

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
        
        Assert.Throws<InvalidOperationException>(() => stream.DeconstructIntoUpdateMessage());
    }
    
    [Fact]
    public void ToStreamRecord_ShouldReturnStreamRecord_WhenMessageIsMarkedSent()
    {
        var hostUrl = "exampleHostUrl";
        var messageId = 12345UL;

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        StreamRecord? existingStream = null;

        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        stream.MarkMessageSent(messageId);

        var result = stream.ToStreamRecord();

        // Assert
        Assert.Null(result.Id);
        Assert.Equal(messageId, result.MessageId);
    }
    
    [Fact]
    public void ToStreamRecord_ShouldThrowException_WhenMessageIdIsNotSaved()
    {
        var hostUrl = "exampleHostUrl";

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        // No existing stream
        StreamRecord? existingStream = null;

        // Current stream is live
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ],
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);

        Assert.Throws<InvalidOperationException>(() => stream.ToStreamRecord());
    }
    
    [Fact]
    public void ToStreamRecord_ShouldReturnStreamRecord_WhenRecordedMessageIdIsPresent()
    {
        var hostUrl = "exampleHostUrl";

        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };
        
        var existingStream = new StreamRecord
        {
            Id = 1,
            MessageId = 12345,
            SubscriptionId = 1
        };

        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        var result = stream.ToStreamRecord();

        Assert.Equal(existingStream.Id, result.Id);
        Assert.Equal(existingStream.SubscriptionId, result.SubscriptionId);
        Assert.Equal(existingStream.MessageId, result.MessageId);
    }

    [Fact]
    public void ToStreamRecord_UpdatesExistingStartTimeEndTime_WhenNewStreamIsStarting()
    {
        var hostUrl = "exampleHostUrl";
        var existingStartTime = new DateTime(2020, 03, 11);
        var existingEndTime = new DateTime(2020, 03, 12);

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
        stream.MarkMessageSent(54321);

        var result = stream.ToStreamRecord();

        Assert.NotEqual(existingStartTime, result.StartTime);
        Assert.NotEqual(existingEndTime, result.EndTime);
        Assert.Null(result.EndTime);
    }
    
    [Fact]
    public void ToStreamRecord_SetsNewMessageId_WhenNewStreamIsStarting()
    {
        var hostUrl = "exampleHostUrl";
        var newMessageId = 54321UL;
        
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
        stream.MarkMessageSent(newMessageId);

        var result = stream.ToStreamRecord();

        Assert.Equal(newMessageId, result.MessageId);
    }
    
    [Fact]
    public void ToStreamRecord_UseExistingStartTime_WhenStreamIsUpdating()
    {
        var hostUrl = "exampleHostUrl";
        var existingStartTime = new DateTime(2020, 03, 11);

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
            StartTime = existingStartTime,
            EndTime = null,
            Status = StreamStatus.Live
        };

        // Current live stream
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        var result = stream.ToStreamRecord();

        Assert.Equal(existingStartTime, result.StartTime);
        Assert.Null(result.EndTime);
    }
    
    [Fact]
    public void ToStreamRecord_UseExistingMessageId_WhenStreamIsUpdating()
    {
        var hostUrl = "exampleHostUrl";

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

        // Current live stream
        var currentStream = new KeySummary
        {
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };

        var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
        var result = stream.ToStreamRecord();

        Assert.Equal(existingStream.MessageId, result.MessageId);
    }
    
    [Fact]
    public void ToStreamRecord_CurrentStreamOffline_WhenTimeSinceLastFrameOutOfWindow()
    {
        var hostUrl = "exampleHostUrl";
        var secondsSinceLastFrame = 30;

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
        var result = stream.ToStreamRecord();

        Assert.Equal(StreamStatus.Offline, result.Status);
    }
    
    // Update viewer count test for update message
}