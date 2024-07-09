using Dapper;
using Mira.Data;

namespace Mira.Features.SlashCommands.Unsubscribe.Repositories;

public class CommandRepository(DbContext context)
{
    public async Task<bool> DeleteSubscriptionAsync(int subscriptionId)
    {
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(
            "DELETE FROM subscription WHERE id = @subscriptionId", new
            {
                subscriptionId
            });

        return result == 1;
    }
}