using Dapper;
using Shared.Core;

namespace Commands.Core.AddHost.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<bool> HostExistsAsync(string hostUrl, ulong guildId)
    {
        var connection = context.CreateConnection();
        var result = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM host WHERE url = @hostUrl AND guild_id = @guildId", new
            {
                hostUrl,
                guildId
            });

        return result > 0;
    }
}