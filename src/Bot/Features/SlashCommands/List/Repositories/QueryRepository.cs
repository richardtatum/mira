using Dapper;
using Mira.Data;
using Mira.Features.SlashCommands.List.Models;

namespace Mira.Features.SlashCommands.List.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<SubscriptionSummary[]> GetSubscriptionsAsync(ulong guildId)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<SubscriptionSummary>(
            @"SELECT h.url host, s.stream_key streamKey 
                FROM host h
                LEFT JOIN subscription s ON s.host_id = h.id
                WHERE h.guild_id = @guildId", new
            {
                guildId
            });

        return results.ToArray();
    }
    
}