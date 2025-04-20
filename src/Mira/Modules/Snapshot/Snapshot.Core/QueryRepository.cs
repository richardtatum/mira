using Dapper;
using Shared.Core;

namespace Snapshot.Core;

public class QueryRepository(DbContext context)
{
    public async Task<string[]> GetLiveStreamKeysAsync(string hostUrl, CancellationToken cancellationToken)
    {
        using var connection = context.CreateConnection();
        var command = new CommandDefinition(
@"SELECT DISTINCT sub.stream_key 
            FROM subscription sub
            INNER JOIN host h ON sub.host_id = h.id
            INNER JOIN stream s ON s.subscription_id = sub.id
            WHERE h.url = @hostUrl
            AND s.status = @live", new
            {
                hostUrl,
                live = StreamStatus.Live
            }, cancellationToken: cancellationToken);

        var results =  await connection.QueryAsync<string>(command);
        return results.ToArray();
    }
}