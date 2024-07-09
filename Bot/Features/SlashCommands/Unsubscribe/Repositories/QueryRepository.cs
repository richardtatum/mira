using Dapper;
using Mira.Data;
using Mira.Features.Shared.Models;

namespace Mira.Features.SlashCommands.Unsubscribe.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<SubscriptionSummary[]> GetSubscriptionsAsync(ulong guildId)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<SubscriptionSummary>(
            @"SELECT s.id, h.url host, s.stream_key streamKey 
                FROM subscription s
                INNER JOIN host h ON s.host_id = h.id
                WHERE h.guild_id = @guildId OR h.guild_id = @global", new
            {
                guildId,
                global = -1
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
                AND h.guild_id = @guildId OR h.guild_id = @global", new
            {
                subscriptionId,
                guildId,
                global = -1
            });
    }
}