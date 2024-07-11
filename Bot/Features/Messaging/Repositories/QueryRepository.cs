using Dapper;
using Mira.Data;

namespace Mira.Features.Messaging.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<StreamSummary> GetStreamSummaryAsync(int subscriptionId)
    {
        using var connection = context.CreateConnection();

        return await connection.QueryFirstAsync<StreamSummary>(
            @"SELECT h.url host, n.stream_key streamKey, n.channel, s.status, s.viewer_count viewerCount, s.start_time startTime, s.end_time endTime
                FROM subscription n
                INNER JOIN host h ON h.id = n.host_id
                INNER JOIN stream s ON s.subscription_id = n.id
                WHERE n.id = @subscriptionId", new
            {
                subscriptionId
            });
    }
}