using Dapper;
using Mira.Data;
using Mira.Features.StreamChecker.Models;

namespace Mira.Features.StreamChecker.Repositories;

public class CommandRepository(DbContext context)
{
    public async Task UpsertStreamRecord(StreamRecord stream)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO stream (subscription_id, status, start_time)
                VALUES (@subscriptionId, @status, @startTime)
                ON CONFLICT (subscription_id) DO UPDATE
                SET status = @status, start_time = @startTime, end_time = @endTime", new
            {
                subscriptionId = stream.SubscriptionId,
                status = stream.Status,
                startTime = stream.StartTime,
                endTime = stream.EndTime
            });
    }
}