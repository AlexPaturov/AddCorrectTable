using AddCorrectTable.Data;
using Dapper;

namespace AddCorrectTable.Services;

public class MaterialService
{
    private readonly FirebirdDbContext _db;
    public MaterialService(FirebirdDbContext db)
    {
        _db = db;
    }

    public async Task<List<AggregatedMaterial>> GetAggregatedMaterialsAsync(DateTime date)
    {
        using var connection = _db.CreateConnection();
        var sql = @"
            SELECT m.KODN, m.NAME, SUM(dv.MASS) AS MassSum
            FROM MATERIAL_DAY_VALUE dv
            JOIN MATERIAL m ON m.KODN = dv.KODN
            WHERE dv.DAT = @Date
            GROUP BY m.KODN, m.NAME
            ORDER BY m.KODN";

        var results = await connection.QueryAsync<AggregatedMaterial>(sql, new { Date = date });
        return results.ToList();
    }

    public async Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials)
    {
        using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        var sql = @"
            UPDATE OR INSERT INTO MATERIAL_AGGREGATED_CORRECTED (KODN, MASS_CORRECTED, DAT)
            VALUES (@Kodn, @MassSum, @Date) MATCHING (KODN, DAT)";

        var result = await connection.ExecuteAsync(sql, materials, transaction);
        await transaction.CommitAsync();
        return result;
    }

    // === НОВЫЙ МЕТОД ===
    public async Task<List<MaterialAggregatedCorrected>> GetCorrectedMaterialsAsync(DateTime date)
    {
        // SQL-запрос, который соединяет две таблицы, чтобы получить имя материала
        var sql = @"
            SELECT 
                c.ID, 
                c.KODN, 
                m.NAME,
                c.MASS_CORRECTED AS MassCorrected, 
                c.DAT, 
                c.COMMENT, 
                c.USERNAME, 
                c.CORRECTED_AT  
            FROM 
                MATERIAL_AGGREGATED_CORRECTED c
            LEFT JOIN 
                MATERIAL m ON c.KODN = m.KODN
            WHERE 
                c.DAT = @Date
            ORDER BY
                m.NAME";

        using var connection = _db.CreateConnection();

        var result = await connection.QueryAsync<MaterialAggregatedCorrected>(sql, new { Date = date });
        return result.ToList();
    }
}
