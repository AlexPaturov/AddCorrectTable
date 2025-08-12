using AddCorrectTable.Data;
using Dapper;

namespace AddCorrectTable.Services;

public class FirebirdMaterialService : IMaterialService
{
    private readonly IDbContext _db;

    public FirebirdMaterialService(IDbContext db)
    {
        _db = db;
    }

    public async Task<List<AggregatedMaterial>> GetAggregatedMaterialsAsync(DateTime date)
    {
        using var connection = await _db.CreateConnectionAsync();
        var sql = @"
        SELECT 
            m.KODN,
            m.NAME,
            SUM(dv.MASS) AS MassSum, 
            c.MASS_CORRECTED AS PreviouslyCorrectedMass
        FROM
            MATERIAL m
        JOIN
            MATERIAL_DAY_VALUE dv ON m.KODN = dv.KODN
        LEFT JOIN
            MATERIAL_AGGREGATED_CORRECTED c ON m.KODN = c.KODN AND dv.DAT = c.DAT
        WHERE
            dv.DAT = @Date
        GROUP BY
            m.KODN, m.NAME, c.MASS_CORRECTED
        ORDER BY
            m.KODN";

        var results = await connection.QueryAsync<AggregatedMaterial>(sql, new { Date = date });
        return results.ToList();
    }

    public async Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials)
    {
        using var connection = await _db.CreateConnectionAsync();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        var sql = @"
            UPDATE OR INSERT INTO MATERIAL_AGGREGATED_CORRECTED (KODN, MASS_CORRECTED, DAT)
            VALUES (@Kodn, @MassSum, @Date) MATCHING (KODN, DAT)";

        var result = await connection.ExecuteAsync(sql, materials, transaction);
        await transaction.CommitAsync();
        return result;
    }

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

        using var connection = await _db.CreateConnectionAsync();

        var result = await connection.QueryAsync<MaterialAggregatedCorrected>(sql, new { Date = date });
        return result.ToList();
    }
}
