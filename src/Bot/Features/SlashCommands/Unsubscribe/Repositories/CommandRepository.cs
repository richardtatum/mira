using Dapper;
using Mira.Data;

namespace Mira.Features.SlashCommands.Unsubscribe.Repositories;

public class CommandRepository(DbContext context)
{
    public async Task<bool> DeleteSubscriptionAsync(int id)
    {
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(
            "DELETE FROM subscription WHERE id = @id", new
            {
                id
            });

        return result == 1;
    }
}