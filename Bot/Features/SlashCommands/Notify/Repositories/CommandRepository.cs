using Dapper;
using Mira.Data;

namespace Mira.Features.SlashCommands.Notify.Repositories;

public class CommandRepository(DbContext context)
{
    internal async Task<int> AddNotification(Notification notification)
    {
        using var connection = context.CreateConnection();
        // Query single allows us to return the ID of the inserted row
        return await connection.QuerySingleAsync<int>(
            @"INSERT INTO notification
                (token, channel, created_by)
                VALUES
                (@token, @channel, @createdBy)
                RETURNING id", new
            {
                token = notification.Token,
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