using Dapper;
using Shared.Core;

namespace Commands.Core.Playing.Repositories;

public class CommandRepository(DbContext context)
{
    public async Task<bool> SetPlayingAsync(int streamId, string playing)
    {
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(
            @"UPDATE stream SET playing = @playing WHERE id = @streamId", new
            {
                streamId,
                playing
            });

        return result > 0;
    }
}