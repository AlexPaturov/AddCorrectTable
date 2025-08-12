using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Data.Common;

namespace AddCorrectTable.Data;

public class FirebirdDbContext : IDbContext
{
    private readonly string _connectionString;

    public FirebirdDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public DbConnection CreateConnection() => new FbConnection(_connectionString);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task<DbConnection> CreateConnectionAsync()
    {
        var connection = new FbConnection(_connectionString);
        return connection;
    }
}
