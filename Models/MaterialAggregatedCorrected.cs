using System.ComponentModel.DataAnnotations.Schema;

namespace AddCorrectTable.Models;

public class MaterialAggregatedCorrected
{
    public long Id { get; set; }
    public int Kodn { get; set; }

    // Добавим свойство Name, чтобы показывать имя, а не код
    [NotMapped] // Это поле не будет маппиться на колонку БД при чтении
    public string Name { get; set; } = string.Empty;

    [Column("MASS_CORRECTED")]
    public decimal? MassCorrected { get; set; }

    [Column("DAT")]
    public DateTime Date { get; set; }

    [Column("COMMENT")]
    public string? Comment { get; set; }

    public string? Username { get; set; }

    [Column("CORRECTED_AT")]
    public DateTime? CorrectedAt { get; set; }
}
