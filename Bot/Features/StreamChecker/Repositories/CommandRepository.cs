using Dapper;
using Mira.Data;
using Mira.Features.Shared.Models;

namespace Mira.Features.StreamChecker.Repositories;

public class CommandRepository(DbContext context)
{
    public async Task UpsertStreamRecord(StreamRecord stream)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO stream (notification_id, status, start_time)
                VALUES (@notificationId, @status, @startTime)
                ON CONFLICT (notification_id) DO UPDATE
                SET status = @status, start_time = @startTime, end_time = @endTime", new
            {
                notificationId = stream.NotificationId,
                status = stream.Status,
                startTime = stream.StartTime,
                endTime = stream.EndTime
            });
    }
}