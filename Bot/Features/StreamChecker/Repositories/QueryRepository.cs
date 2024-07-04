using Dapper;
using Mira.Data;
using Mira.Features.Shared.Models;

namespace Mira.Features.StreamChecker.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<StreamRecord[]> GetLiveStreamsAsync(IEnumerable<int> notificationIds)
    {
        using var connection = context.CreateConnection();

        var results = await connection.QueryAsync<StreamRecord>(
            @"SELECT id, notification_id notificationId, status, start_time startTime, end_time endTime
                FROM stream
                WHERE notification_id IN (@notificationIds) AND status = @live", new
            {
                notificationIds,
                live = StreamStatus.Live
            });

        return results.ToArray();
    }

    internal async Task<NotificationSummary> GetNotificationSummaryAsync(int notificationId)
    {
        using var connection = context.CreateConnection();

        return await connection.QueryFirstAsync<NotificationSummary>(
            @"SELECT h.url host, n.stream_key streamKey, n.channel, s.start_time startTime, s.end_time endTime
                FROM notification n
                INNER JOIN host h ON h.id = n.host_id
                INNER JOIN stream s ON s.notification_id = n.id
                WHERE n.id = @notificationId", new
            {
                notificationId
            });
    }

    internal async Task<Host[]> GetHostsAsync()
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Host>(
            @"SELECT id, url, guild_id guildId FROM host");

        return results.ToArray();
    }

    internal async Task<Notification[]> GetNotificationsAsync(int hostId)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Notification>(
            @"SELECT id, stream_key streamKey, host_id hostId, channel, created_by createdBy 
                FROM notification
                WHERE host_id = @hostId", new
        {
            hostId
        });

        return results.ToArray();
    }
}