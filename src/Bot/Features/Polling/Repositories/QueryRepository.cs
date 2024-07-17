using Dapper;
using Mira.Data;
using Mira.Features.Polling.Models;
using Mira.Features.Shared;

namespace Mira.Features.Polling.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<StreamOverview[]> GetLiveStreamsAsync(IEnumerable<int> subscriptionIds)
    {
        using var connection = context.CreateConnection();

        var results = await connection.QueryAsync<StreamOverview>(
            @"SELECT 
                    s.id,
                    s.subscription_id subscriptionId,
                    sub.stream_key streamKey,
                    h.url hostUrl,
                    s.status,
                    s.viewer_count viewerCount,
                    s.start_time startTime,
                    s.end_time endTime,
                    s.message_id messageId,
                    sub.channel_id channelId
                FROM stream s
                INNER JOIN subscription sub ON sub.id = s.subscription_id
                INNER JOIN host h ON h.id = sub.host_id
                WHERE s.subscription_id IN @subscriptionIds AND s.status = @live", new
            {
                subscriptionIds,
                live = StreamStatus.Live
            });

        return results.ToArray();
    }

    internal async Task<StreamOverview[]> GetStreamsAsync(string hostUrl)
    {
        using var connection = context.CreateConnection();

        var results = await connection.QueryAsync<StreamOverview>(
            @"SELECT 
                    s.id,
                    s.subscription_id subscriptionId,
                    sub.stream_key streamKey,
                    h.url hostUrl,
                    s.status,
                    s.viewer_count viewerCount,
                    s.start_time startTime,
                    s.end_time endTime,
                    s.message_id messageId,
                    sub.channel_id channelId
                FROM stream s
                INNER JOIN subscription sub ON sub.id = s.subscription_id
                INNER JOIN host h ON h.id = sub.host_id
                WHERE h.url = @hostUrl", new
            {
                hostUrl
            });

        return results.ToArray();
    }

    public async Task<ulong?> GetChannelIdAsync(string hostUrl, string streamKey)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<ulong>(
            @"SELECT channel_id 
                FROM subscription s
                INNER JOIN host h ON h.id = s.host_id
                WHERE s.stream_key = @streamKey AND h.url = @hostUrl", new
            {
                hostUrl,
                streamKey
            });
    }


    internal async Task<Host[]> GetHostsAsync()
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Host>(
            @"SELECT url, MIN(poll_interval_seconds) pollIntervalSeconds
                FROM host
                GROUP BY url"
        );

        return results.ToArray();
    }

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
}