using Dapper;
using Mira.Data;
using Mira.Features.Shared.Models;

namespace Mira.Features.SlashCommands.Notify.Repositories;

public class CommandRepository(DbContext context)
{
    internal async Task<int> AddNotification(Notification notification)
    {
        using var connection = context.CreateConnection();
        // Query single allows us to return the ID of the inserted row
        return await connection.QuerySingleAsync<int>(
            @"INSERT INTO notification
                (stream_key, channel, created_by)
                VALUES
                (@streamKey, @channel, @createdBy)
                RETURNING id", new
            {
                streamKey = notification.StreamKey,
                channel = notification.Channel,
                createdBy = notification.CreatedBy
            });
    }

    internal async Task UpdateNotification(int notificationId, int hostId)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE notification SET host_id = @hostId WHERE id = @notificationId", new
            {
                hostId,
                notificationId
            });
    }
}