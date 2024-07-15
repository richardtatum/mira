using Dapper;
using Mira.Data;
using Mira.Features.SlashCommands.Subscribe.Models;

namespace Mira.Features.SlashCommands.Subscribe.Repositories;

public class CommandRepository(DbContext context)
{
    internal async Task<bool> AddSubscription(Subscription subscription)
    {
        using var connection = context.CreateConnection();

        // Query single allows us to return the ID of the inserted row
        var result = await connection.ExecuteAsync(
            @"INSERT INTO subscription
                (host_id, stream_key, channel_id, created_by)
                VALUES
                (@hostId, @streamKey, @channelId, @createdBy)
                RETURNING id", new
            {
                hostId = subscription.HostId,
                streamKey = subscription.StreamKey,
                channelId = subscription.ChannelId,
                createdBy = subscription.CreatedBy
            });

        return result == 1;
    }
}