using Dapper;
using Mira.Data;
using Mira.Features.SlashCommands.Subscribe.Models;

namespace Mira.Features.SlashCommands.Subscribe.Repositories;

public class CommandRepository(DbContext context)
{
    internal async Task<int> AddSubscription(Subscription subscription)
    {
        using var connection = context.CreateConnection();

        // Query single allows us to return the ID of the inserted row
        return await connection.QuerySingleAsync<int>(
            @"INSERT INTO subscription
                (stream_key, channel, created_by)
                VALUES
                (@streamKey, @channel, @createdBy)
                RETURNING id", new
            {
                streamKey = subscription.StreamKey,
                channel = subscription.Channel,
                createdBy = subscription.CreatedBy
            });
    }

    internal async Task UpdateSubscription(int subscriptionId, int hostId)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE subscription SET host_id = @hostId WHERE id = @subscriptionId", new
            {
                hostId, subscriptionId = subscriptionId
            });
    }
}