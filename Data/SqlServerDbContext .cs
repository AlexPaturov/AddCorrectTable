using System.Data.Common;
using System.Data.SqlClient;

namespace AddCorrectTable.Data;

public class SqlServerDbContext : IDbContext
{

    private readonly string _connectionString;

    public SqlServerDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConnection")!;
    }

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<DbConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        return connection;
    }
}
