using Dapper;
using Shared.Core;

namespace Snapshot.Core;

public class CommandRepository(DbContext context)
{
    public async Task SaveSnapshotAsync(string streamKey, byte[] snapshot, CancellationToken cancellationToken)
    {
        var connection = context.CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE stream
                SET snapshot = @snapshot
                WHERE subscription_id IN (
                    SELECT id
                    FROM subscription
                    WHERE stream_key = @streamKey
                )", new
            {
                streamKey,
                snapshot
            }, cancellationToken: cancellationToken);
        
        await connection.ExecuteAsync(command);
    }
}