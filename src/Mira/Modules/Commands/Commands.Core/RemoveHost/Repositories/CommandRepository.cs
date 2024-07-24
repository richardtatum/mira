using Dapper;
using Shared.Core;

namespace Commands.Core.RemoveHost.Repositories;

public class CommandRepository(DbContext context)
{
    public async Task<bool> DeleteHostAsync(int id)
    {
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(
            "DELETE FROM host WHERE id = @id", new
            {
                id
            });

        return result == 1;
    }
}