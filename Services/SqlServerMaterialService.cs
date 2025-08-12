
using AddCorrectTable.Data;
using Dapper;

namespace AddCorrectTable.Services;

public class SqlServerMaterialService : IMaterialService
{
    private readonly IDbContext _dbContext;

    public SqlServerMaterialService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AggregatedMaterial>> GetAggregatedMaterialsAsync(DateTime date)
    {
        var sql = @"
            SELECT 
                m.KODN,
                m.NAME,
                SUM(dv.MASS) AS MassSum,
                MAX(c.MASS_CORRECTED) AS PreviouslyCorrectedMass
            FROM 
                MATERIAL m
            JOIN 
                MATERIAL_DAY_VALUE dv ON m.KODN = dv.KODN
            LEFT JOIN 
                MATERIAL_AGGREGATED_CORRECTED c ON m.KODN = c.KODN AND dv.DAT = c.DAT
            WHERE 
                dv.DAT = @Date
            GROUP BY 
                m.KODN, m.NAME
            ORDER BY
                m.KODN";

        await using var connection = _dbContext.CreateConnection();
        var result = await connection.QueryAsync<AggregatedMaterial>(sql, new { Date = date });
        return result.ToList();
    }

    public async Task<List<MaterialAggregatedCorrected>> GetCorrectedMaterialsAsync(DateTime date)
    {
        // === T-SQL ВЕРСИЯ ЗАПРОСА ===
        var sql = @"
            SELECT 
                c.ID, 
                c.KODN, 
                m.NAME,
                c.MASS_CORRECTED AS MassCorrected, 
                c.DAT, 
                c.[COMMENT], -- Берем в скобки, так как COMMENT - зарезервированное слово
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

        await using var connection = _dbContext.CreateConnection();
        var result = await connection.QueryAsync<MaterialAggregatedCorrected>(sql, new { Date = date });
        return result.ToList();
    }

    // public async Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials, string? userName)
    public async Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials)
    {
        await using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var sql = @"
                MERGE INTO MATERIAL_AGGREGATED_CORRECTED AS Target
                USING (SELECT @Kodn AS KODN, @Date AS DAT) AS Source
                ON Target.KODN = Source.KODN AND Target.DAT = Source.DAT
                WHEN MATCHED THEN
                    UPDATE SET 
                        MASS_CORRECTED = @MassSum,
                        USERNAME = @UserName,
                        CORRECTED_AT = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (KODN, MASS_CORRECTED, DAT, USERNAME, CORRECTED_AT)
                    VALUES (@Kodn, @MassSum, @Date, @UserName, GETDATE());";

            var dataToSave = materials.Select(m => new {
                m.Kodn,
                m.MassSum,
                m.Date,
                //UserName = userName
                UserName = Environment.UserName ?? "UnknownUser" // Используем Environment.UserName для получения имени пользователя

            });

            var result = await connection.ExecuteAsync(sql, dataToSave, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            // Rollback будет вызван автоматически при выходе из await using
            throw;
        }
    }
}
