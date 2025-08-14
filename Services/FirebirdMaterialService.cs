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

    /// <summary>
    /// Получаю агрегированные материалы за указанный период.
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public async Task<List<AggregatedMaterial>> GetAggregatedMaterialsAsync(DateTime startDate, DateTime endDate)
    {
        var sql = @"
            WITH 
            FactMass AS (
                SELECT KODN, SUM(MASS) as TotalFactMass
                FROM MATERIAL_DAY_VALUE
                WHERE DAT BETWEEN @StartDate AND @EndDate
                GROUP BY KODN
            ),
            CorrectedMass AS (
                SELECT KODN, SUM(MASS_CORRECTED) as TotalCorrectedMass
                FROM MATERIAL_AGGREGATED_CORRECTED
                WHERE DAT BETWEEN @StartDate AND @EndDate
                GROUP BY KODN
            )
            SELECT
                m.KODN,
                m.NAME,
                fm.TotalFactMass AS MassSum,
                cm.TotalCorrectedMass AS PreviouslyCorrectedMass
            FROM
                FactMass fm
            FULL JOIN
                CorrectedMass cm ON fm.KODN = cm.KODN
            JOIN
                MATERIAL m ON m.KODN = COALESCE(fm.KODN, cm.KODN)
            ORDER BY
                m.KODN";

        using var _connection = await _db.CreateConnectionAsync();
        var materials = (await _connection.QueryAsync<AggregatedMaterial>(
            sql,
            new { StartDate = startDate, EndDate = endDate }
        )).ToList();

        // Сбрасываем флаг изменения после загрузки
        foreach (var m in materials)
            m.ResetModifiedState();

        return materials;
    }

    /// <summary>
    /// Сохраняет скорректированные данные материалов.
    /// Если выбран диапазон дат, равномерно распределяет массу по дням,
    /// прибавляя остаток от деления к последнему дню для сохранения точности.
    /// </summary>
    /// <param name="materials">Коллекция материалов для сохранения.</param>
    /// <param name="startDate">Начальная дата диапазона.</param>
    /// <param name="endDate">Конечная дата диапазона.</param>
    /// <returns>Количество измененных строк в базе данных.</returns>
    public async Task<int> SaveCorrectedMaterialsAsync(IEnumerable<AggregatedMaterial> materials, DateTime startDate, DateTime endDate, string? userName)
    {
        // 1. Вычисляем количество дней в выбранном диапазоне.
        int numberOfDays = (endDate.Date - startDate.Date).Days + 1;
        if (numberOfDays <= 0)
        {
            return 0; // Защищаемся от некорректного диапазона дат.
        }

        // 2. Устанавливаем соединение с базой данных и начинаем транзакцию.
        // 'await using' гарантирует, что соединение и транзакция будут корректно закрыты
        // и транзакция будет отменена (Rollback) в случае ошибки.
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 3. SQL-запрос для Firebird (UPSERT).
            var sql = @"
            UPDATE OR INSERT INTO MATERIAL_AGGREGATED_CORRECTED 
                (KODN, MASS_CORRECTED, DAT, USERNAME, CORRECTED_AT)
            VALUES 
                (@Kodn, @MassCorrected, @Date, @UserName, CURRENT_TIMESTAMP) 
            MATCHING (KODN, DAT)";

            int totalAffectedRows = 0;

            // 4. Проходим по каждому материалу, который нужно сохранить.
            foreach (var material in materials)
            {
                // Пропускаем материалы без массы.
                if (!material.MassSum.HasValue)
                {
                    continue;
                }

                decimal totalMass = material.MassSum.Value;

                // 5. Алгоритм распределения массы с остатком.
                // Вычисляем базовую порцию, округляя ВНИЗ до 3 знаков.
                decimal basePortionPerDay = Math.Truncate(totalMass / numberOfDays * 1000) / 1000;
                // Вычисляем остаток, который не распределился из-за округления.
                decimal remainder = totalMass - (basePortionPerDay * numberOfDays);

                // 6. В цикле сохраняем порцию для каждого дня в диапазоне.
                for (int i = 0; i < numberOfDays; i++)
                {
                    var currentDate = startDate.Date.AddDays(i);
                    decimal massForThisDay = basePortionPerDay;

                    // Если это последний день в цикле, добавляем к его порции весь остаток.
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

            // 7. Если все прошло успешно, подтверждаем транзакцию.
            await transaction.CommitAsync();
            return totalAffectedRows;
        }
        catch (Exception ex)
        {
            // В случае любой ошибки 'await using' автоматически сделает Rollback.
            // Здесь мы можем залогировать ошибку и пробросить ее дальше.
            // _logger.LogError(ex, "Ошибка при сохранении скорректированных материалов.");
            throw;
        }
    }

}
