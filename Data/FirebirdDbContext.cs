using FirebirdSql.Data.FirebirdClient;
using System.Data;

namespace AddCorrectTable.Data;

public class FirebirdDbContext
{
    private readonly string _connectionString;
    public FirebirdDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public FbConnection CreateConnection() => new FbConnection(_connectionString);
}
