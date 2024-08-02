using ChangeTracking.Core.Models;
using Dapper;
using Shared.Core;

namespace ChangeTracking.Core.Repositories;

internal class QueryRepository(DbContext context)
{
    internal async Task<Subscription[]> GetSubscriptionsAsync(string hostUrl)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Subscription>(
            @"SELECT s.id, s.stream_key streamKey, s.channel_id channelId
                FROM subscription s
                INNER JOIN host h ON s.host_id = h.id
                WHERE h.url = @hostUrl", new
            {
                hostUrl
            });

        return results.ToArray();
    }

    internal async Task<StreamRecord[]> GetStreamsAsync(IEnumerable<int> subscriptionIds)
    {
        using var connection = context.CreateConnection();

        var results = await connection.QueryAsync<StreamRecord>(
            @"SELECT 
                    id,
                    subscription_id subscriptionId,
                    status,
                    viewer_count viewerCount,
                    start_time startTime,
                    end_time endTime,
                    message_id messageId,
                    playing,
                    snapshot
                FROM stream s
                WHERE s.subscription_id in @subscriptionIds ", new
            {
                subscriptionIds
            });

        return results.ToArray();
    }
}