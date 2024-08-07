using ChangeTracking.Core.Models;
using Dapper;
using Shared.Core;

namespace ChangeTracking.Core.Repositories;

internal class CommandRepository(DbContext context)
{
    internal async Task UpsertStreamRecord(StreamRecord stream)
    {
        // Conflicts could either be because of current live streams where we are updating the status/viewers/endtime
        // or existing records that are now being overwritten with new streams
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO stream (subscription_id, status, message_id, viewer_count, start_time)
                VALUES (@subscriptionId, @status, @messageId, @viewerCount, @startTime)
                ON CONFLICT (subscription_id) DO UPDATE
                SET status = @status, 
                    message_id = @messageId, 
                    viewer_count = @viewerCount, 
                    start_time = @startTime, 
                    end_time = @endTime,
                    playing = @playing,
                    snapshot = @snapshot", new
            {
                subscriptionId = stream.SubscriptionId,
                status = stream.Status,
                messageId = stream.MessageId,
                viewerCount = stream.ViewerCount,
                startTime = stream.StartTime,
                endTime = stream.EndTime,
                playing = stream.Playing,
                snapshot = stream.Snapshot
            });
    }
}