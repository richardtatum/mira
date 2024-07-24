using Dapper;
using Shared.Core;

namespace Cleanup.Core;

public class CommandRepository(DbContext context)
{
    public async Task CleanupGuildAsync(ulong guildId)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM host WHERE guild_id = @guildId", new
            {
                guildId
            });
    }

    public async Task CleanupChannelAsync(ulong channelId)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM subscription WHERE channel_id = @channelId", new
            {
                channelId
            });
    }
}