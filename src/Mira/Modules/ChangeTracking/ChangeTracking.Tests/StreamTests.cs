using ChangeTracking.Core.Models;
using Shared.Core;
using Stream = ChangeTracking.Core.Models.Stream;

namespace ChangeTracking.Tests;

public class StreamTests
{
    [Fact(DisplayName = "Ensure loading subscriptions, existing streams and current streams is required.")]
    public void MissingLoadedData_IsValidReturnsFalse()
    {
        var hostUrl = "fakeHostUrl";

        var noData = new Stream(hostUrl);
        
        var missingSubscriptionData= new Stream(hostUrl)
            .LoadExistingStreamData(new StreamRecord())
            .LoadCurrentStreamData(new KeySummary());

        var missingExistingStreamData = new Stream(hostUrl)
            .LoadSubscriptionData(new Subscription())
            .LoadCurrentStreamData(new KeySummary());
        
        var missingCurrentStreamData = new Stream(hostUrl)
            .LoadSubscriptionData(new Subscription())
            .LoadExistingStreamData(new StreamRecord());
        
        Assert.False(noData.IsValid);
        Assert.False(missingSubscriptionData.IsValid);
        Assert.False(missingExistingStreamData.IsValid);
        Assert.False(missingCurrentStreamData.IsValid);
    }
    
    [Fact(DisplayName = "Ensure streams without existing records are valid")]
    public void NullExistingData_IsValidReturnsTrue()
    {
        var hostUrl = "fakeHostUrl";
        
        var stream= new Stream(hostUrl)
            .LoadSubscriptionData(new Subscription())
            .LoadExistingStreamData(null)
            .LoadCurrentStreamData(new KeySummary());
        
        Assert.True(stream.IsValid);
    }
    
    [Fact]
    public void DeconstructIntoNewMessage_ShouldReturnCorrectTuple_WhenStreamIsStarting()
    {
        // Arrange
        var stream = new Stream("http://example.com");
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

        stream.LoadSubscriptionData(subscription);
        stream.LoadCurrentStreamData(currentStream);
        stream.LoadExistingStreamData(null);

        // Act
        var result = stream.DeconstructIntoNewMessage();

        // Assert
        Assert.Equal(subscription.ChannelId, result.channelId);
        Assert.Equal(currentStream.ViewerCount, result.viewerCount);
    }
    
    [Fact]
    public void DeconstructIntoNewMessage_ShouldThrowException_WhenStreamIsNotStarting()
    {
        // Arrange
        var stream = new Stream("http://example.com");
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

        var existingStream = new StreamRecord
        {
            Status = StreamStatus.Live
        };

        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(existingStream);
        stream.LoadCurrentStreamData(currentStream);

        // Act
        Assert.Throws<InvalidOperationException>(() => stream.DeconstructIntoNewMessage());
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldReturnCorrectTuple_WhenStreamIsLive()
    {
        // Arrange
        var stream = new Stream("http://example.com");
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
            ]
        };
        
        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(existingStream);
        stream.LoadCurrentStreamData(currentStream);
        
        // Act
        var result = stream.DeconstructIntoUpdateMessage();

        // Assert
        Assert.Equal(existingStream.MessageId, result.messageId);
        Assert.Equal(StreamStatus.Live, result.status);
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldReturnCorrectTuple_WhenStreamIsEnding()
    {
        var stream = new Stream("http://example.com");
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

        // Current stream is offline
        var currentStream = new KeySummary();
        
        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(existingStream);
        stream.LoadCurrentStreamData(currentStream);
        
        var result = stream.DeconstructIntoUpdateMessage();

        Assert.Equal(existingStream.MessageId, result.messageId);
        Assert.Equal(StreamStatus.Offline, result.status);
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldThrowException_WhenStreamIsOffline()
    {
        var stream = new Stream("http://example.com");
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

        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(existingStream);
        stream.LoadCurrentStreamData(currentStream);
        
        Assert.Throws<InvalidOperationException>(() => stream.DeconstructIntoUpdateMessage());
    }
    
    [Fact]
    public void DeconstructIntoUpdateMessage_ShouldThrowException_WhenNoExistingStream()
    {
        var stream = new Stream("http://example.com");
        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        // Current stream is offline
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ]
        };
        
        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(null);
        stream.LoadCurrentStreamData(currentStream);
        
        Assert.Throws<InvalidOperationException>(() => stream.DeconstructIntoUpdateMessage());
    }
    
    [Fact(DisplayName = "Ensure creating a new stream record works when message is marked as sent")]
    public void ToStreamRecord_ShouldReturnStreamRecord_WhenMessageIsMarkedSent()
    {
        var stream = new Stream("http://example.com");
        var messageId = 12345UL;
        var viewerCount = 150;
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
            ],
            WhepSessions = Enumerable.Range(0, viewerCount).Select(_ => new Viewer()).ToArray()
        };

        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(null);
        stream.LoadCurrentStreamData(currentStream);
        stream.MarkMessageSent(messageId);

        var result = stream.ToStreamRecord();

        // Assert
        Assert.Null(result.Id);
        Assert.Equal(subscription.Id, result.SubscriptionId);
        Assert.Equal(messageId, result.MessageId);
        Assert.Equal(viewerCount, result.ViewerCount);
    }
    
    [Fact(DisplayName = "Ensure creating a new stream record throws an error when no message id is stored")]
    public void ToStreamRecord_ShouldThrowException_WhenMessageIdIsNotSaved()
    {
        var stream = new Stream("http://example.com");
        var subscription = new Subscription
        {
            Id = 1,
            StreamKey = "streamkey",
            ChannelId = 54321
        };

        // Current stream is live
        var currentStream = new KeySummary
        {
            FirstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            VideoStreams = [
                new VideoStream { LastKeyFrameSeen = DateTime.UtcNow }
            ],
        };

        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(null);
        stream.LoadCurrentStreamData(currentStream);

        Assert.Throws<InvalidOperationException>(() => stream.ToStreamRecord());
    }
    
    [Fact(DisplayName = "Ensure updating an existing stream record works")]
    public void ToStreamRecord_ShouldReturnStreamRecord_WhenRecordedMessageIdIsPresent()
    {
        var stream = new Stream("http://example.com");
        var viewerCount = 100;

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
            ],
            WhepSessions = Enumerable.Range(0, viewerCount).Select(_ => new Viewer()).ToArray()
        };

        stream.LoadSubscriptionData(subscription);
        stream.LoadExistingStreamData(existingStream);
        stream.LoadCurrentStreamData(currentStream);

        var result = stream.ToStreamRecord();

        Assert.Equal(existingStream.Id, result.Id);
        Assert.Equal(existingStream.SubscriptionId, result.SubscriptionId);
        Assert.Equal(existingStream.MessageId, result.MessageId);
        Assert.Equal(viewerCount, result.ViewerCount);
    }

    [Fact]
    public void ToStreamRecord_UpdatesExistingStartTimeEndTime_WhenNewStreamIsStarting()
    {
        var stream = new Stream("http://example.com");
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

        stream.LoadExistingStreamData(existingStream);
        stream.LoadSubscriptionData(subscription);
        stream.LoadCurrentStreamData(currentStream);
        stream.MarkMessageSent(54321);

        var result = stream.ToStreamRecord();

        Assert.NotEqual(existingStartTime, result.StartTime);
        Assert.NotEqual(existingEndTime, result.EndTime);
        Assert.Null(result.EndTime);
    }
    
    [Fact]
    public void ToStreamRecord_SetsNewMessageId_WhenNewStreamIsStarting()
    {
        var stream = new Stream("http://example.com");
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

        stream.LoadExistingStreamData(existingStream);
        stream.LoadSubscriptionData(subscription);
        stream.LoadCurrentStreamData(currentStream);
        stream.MarkMessageSent(newMessageId);

        var result = stream.ToStreamRecord();

        Assert.Equal(newMessageId, result.MessageId);
    }
    
    [Fact]
    public void ToStreamRecord_UseExistingStartTime_WhenStreamIsUpdating()
    {
        var stream = new Stream("http://example.com");
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

        stream.LoadExistingStreamData(existingStream);
        stream.LoadSubscriptionData(subscription);
        stream.LoadCurrentStreamData(currentStream);

        var result = stream.ToStreamRecord();

        Assert.Equal(existingStartTime, result.StartTime);
        Assert.Null(result.EndTime);
    }
    
    [Fact]
    public void ToStreamRecord_UseExistingMessageId_WhenStreamIsUpdating()
    {
        var stream = new Stream("http://example.com");
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

        stream.LoadExistingStreamData(existingStream);
        stream.LoadSubscriptionData(subscription);
        stream.LoadCurrentStreamData(currentStream);

        var result = stream.ToStreamRecord();

        Assert.Equal(existingStream.MessageId, result.MessageId);
    }
    
    [Fact]
    public void ToStreamRecord_CurrentStreamOffline_WhenTimeSinceLastFrameOutOfWindow()
    {
        var stream = new Stream("http://example.com");
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

        stream.LoadExistingStreamData(existingStream);
        stream.LoadSubscriptionData(subscription);
        stream.LoadCurrentStreamData(currentStream);

        var result = stream.ToStreamRecord();

        Assert.Equal(StreamStatus.Offline, result.Status);
    }
    
}