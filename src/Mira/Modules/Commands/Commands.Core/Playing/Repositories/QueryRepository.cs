using Dapper;
using Shared.Core;

namespace Commands.Core.Playing.Repositories;

public class QueryRepository(DbContext context)
{

    public async Task<Stream[]> GetLiveStreamsAsync(ulong guildId)
    {
        using var connection = context.CreateConnection();
        var result = await connection.QueryAsync<Stream>(
            @"SELECT s.id, h.url hostUrl, sub.stream_key key
                FROM stream s
                INNER JOIN subscription sub ON sub.id = s.subscription_id
                INNER JOIN host h on h.id = sub.host_id
                WHERE s.status = @live
                AND h.guild_id = @guildId", new
            {
                live = StreamStatus.Live,
                guildId
            });

        return result.ToArray();
    }

    public async Task<Stream?> GetStreamAsync(int streamId)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<Stream?>(
            @"SELECT s.id, h.url hostUrl, sub.stream_key key
                FROM stream s
                INNER JOIN subscription sub ON sub.id = s.subscription_id
                INNER JOIN host h on h.id = sub.host_id
                WHERE s.id = @streamId", new
            {
                streamId
            });;
    }
    
}