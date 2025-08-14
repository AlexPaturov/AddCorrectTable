
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

    /// <summary>
    /// Асинхронно получает агрегированные данные по материалам за указанный диапазон дат.
    /// </summary>
    /// <param name="startDate">Начальная дата диапазона.</param>
    /// <param name="endDate">Конечная дата диапазона.</param>
    /// <returns>Список агрегированных материалов.</returns>
    public async Task<List<AggregatedMaterial>> GetAggregatedMaterialsAsync(DateTime startDate, DateTime endDate)
    {
        // === SQL-ЗАПРОС НА T-SQL С ПРЕДВАРИТЕЛЬНОЙ АГРЕГАЦИЕЙ ===
        var sql = @"
        -- CTE (Common Table Expression) для агрегации фактической массы
        WITH FactMass AS (
            SELECT
                KODN,
                SUM(MASS) as TotalFactMass
            FROM
                MATERIAL_DAY_VALUE
            WHERE
                DAT BETWEEN @StartDate AND @EndDate -- Используем диапазон
            GROUP BY
                KODN
        ),
        -- CTE для агрегации скорректированной массы
        CorrectedMass AS (
            SELECT
                KODN,
                SUM(MASS_CORRECTED) as TotalCorrectedMass
            FROM
                MATERIAL_AGGregated_CORRECTED
            WHERE
                DAT BETWEEN @StartDate AND @EndDate -- Используем диапазон
            GROUP BY
                KODN
        )
        -- Финальный SELECT, который соединяет уже готовые агрегаты
        SELECT 
            m.KODN,
            m.NAME,
            fm.TotalFactMass AS MassSum,
            cm.TotalCorrectedMass AS PreviouslyCorrectedMass
        FROM 
            MATERIAL m
        LEFT JOIN 
            FactMass fm ON m.KODN = fm.KODN
        LEFT JOIN
            CorrectedMass cm ON m.KODN = cm.KODN
        -- Включаем в результат только те материалы, по которым есть хоть какие-то данные за период
        WHERE 
            fm.TotalFactMass IS NOT NULL OR cm.TotalCorrectedMass IS NOT NULL
        ORDER BY
            m.KODN";

        await using var connection = _dbContext.CreateConnection();
        var result = await connection.QueryAsync<AggregatedMaterial>(sql, new { StartDate = startDate, EndDate = endDate });
        return result.ToList();
    }

    // Services/SqlServerMaterialService.cs

    /// <summary>
    /// Сохраняет скорр. данные в MS SQL. Если выбран диапазон, равномерно распределяет массу по дням,
    /// прибавляя остаток от деления к последнему дню для сохранения точности.
    /// </summary>
    public async Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials, DateTime startDate, DateTime endDate, string? userName)
    {
        // 1. Вычисляем количество дней.
        int numberOfDays = (endDate.Date - startDate.Date).Days + 1;
        if (numberOfDays <= 0)
        {
            return 0;
        }

        // 2. Устанавливаем соединение и начинаем транзакцию.
        await using var connection = _dbContext.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 3. SQL-ЗАПРОС ДЛЯ MS SQL SERVER (MERGE).
            // Используем стандартную для T-SQL команду "UPSERT".
            var sql = @"
            MERGE INTO MATERIAL_AGGREGATED_CORRECTED AS Target
            USING (SELECT @Kodn AS KODN, @Date AS DAT) AS Source
            ON Target.KODN = Source.KODN AND Target.DAT = Source.DAT
            -- Если запись найдена (WHEN MATCHED):
            WHEN MATCHED THEN
                UPDATE SET 
                    MASS_CORRECTED = @MassCorrected,
                    USERNAME = @UserName,
                    CORRECTED_AT = GETDATE() -- Функция получения текущей даты в T-SQL
            -- Если запись не найдена (WHEN NOT MATCHED):
            WHEN NOT MATCHED THEN
                INSERT (KODN, MASS_CORRECTED, DAT, USERNAME, CORRECTED_AT)
                VALUES (@Kodn, @MassCorrected, @Date, @UserName, GETDATE());";

            int totalAffectedRows = 0;

            // 4. Проходим по каждому материалу.
            foreach (var material in materials)
            {
                if (!material.MassSum.HasValue)
                {
                    continue;
                }

                decimal totalMass = material.MassSum.Value;

                // 5. Алгоритм распределения массы с остатком.
                decimal basePortionPerDay = Math.Truncate(totalMass / numberOfDays * 1000) / 1000;
                decimal remainder = totalMass - (basePortionPerDay * numberOfDays);

                // 6. В цикле сохраняем порцию для каждого дня.
                for (int i = 0; i < numberOfDays; i++)
                {
                    var currentDate = startDate.Date.AddDays(i);
                    decimal massForThisDay = basePortionPerDay;

                    if (i == numberOfDays - 1)
                    {
                        massForThisDay += remainder;
                    }

                    var parameters = new
                    {
                        Kodn = material.Kodn,
                        MassCorrected = massForThisDay,
                        Date = currentDate,
                        UserName = userName
                    };

                    totalAffectedRows += await connection.ExecuteAsync(sql, parameters, transaction);
                }
            }

            // 7. Подтверждаем транзакцию.
            await transaction.CommitAsync();
            return totalAffectedRows;
        }
        catch (Exception ex)
        {
            // В случае ошибки 'await using' автоматически сделает Rollback.
            // _logger.LogError(ex, "Ошибка при сохранении скорректированных материалов в MS SQL.");
            throw;
        }
    }
}
