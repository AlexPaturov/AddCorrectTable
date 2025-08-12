using System.Data;
using System.Data.Common;

namespace AddCorrectTable.Data;

public interface IDbContext
{
    /// <summary>
    /// Создает и возвращает открытое подключение к базе данных.
    /// </summary>
    DbConnection CreateConnection();

    /// <summary>
    /// Асинхронно создает и возвращает открытое подключение к базе данных.
    /// </summary>
    Task<DbConnection> CreateConnectionAsync();
}
