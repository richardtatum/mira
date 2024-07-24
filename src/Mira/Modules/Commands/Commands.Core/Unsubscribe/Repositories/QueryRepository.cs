using Commands.Core.Unsubscribe.Models;
using Dapper;
using Shared.Core;

namespace Commands.Core.Unsubscribe.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<SubscriptionSummary[]> GetSubscriptionsAsync(ulong guildId)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<SubscriptionSummary>(
            @"SELECT s.id, h.url host, s.stream_key streamKey 
                FROM subscription s
                INNER JOIN host h ON s.host_id = h.id
                WHERE h.guild_id = @guildId", new
            {
                guildId
            });

        return results.ToArray();
    }

    internal async Task<SubscriptionSummary?> GetSubscriptionAsync(int subscriptionId, ulong guildId)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<SubscriptionSummary?>(
            @"SELECT s.id, h.url host, s.stream_key streamKey 
                FROM subscription s
                INNER JOIN host h ON s.host_id = h.id
                WHERE s.id = @subscriptionId
                AND h.guild_id = @guildId", new
            {
                subscriptionId,
                guildId
            });
    }
}