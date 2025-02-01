using Dapper;
using Shared.Core;

namespace Commands.Core.Playing.Repositories;

public class CommandRepository(DbContext context)
{
    public async Task<bool> SetPlayingAsync(string streamKey, string playing)
    {
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(
            @"UPDATE stream SET playing = @playing
                WHERE subscription_id IN (
                    SELECT id
                    FROM subscription
                    WHERE stream_key = @streamKey
                )", new
            {
                streamKey,
                playing
            });

        return result > 0;
    }
}