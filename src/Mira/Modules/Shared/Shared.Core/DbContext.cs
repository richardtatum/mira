using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Shared.Core;

public class DbContext
{
    private readonly IConfiguration _configuration;

    public DbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connection = _configuration.GetConnectionString("Primary");
        return new SqliteConnection(connection);
    }

    public async Task InitAsync()
    {
        using var connection = CreateConnection();
        var sql = await File.ReadAllTextAsync("Data/init.sql");
        await connection.ExecuteAsync(sql);
    }
}