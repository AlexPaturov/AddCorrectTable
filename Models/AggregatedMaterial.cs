using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddCorrectTable.Models;

public class AggregatedMaterial
{
    public string Kodn { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    private decimal _massSum;

    [Required(ErrorMessage = "Масса не может быть пустой")]
    [Range(0.0, 999999.999, ErrorMessage = "Масса должна быть положительным числом")]
    public decimal MassSum
    {
        get => _massSum;
        set
        {
            // Если новое значение не равно текущему, помечаем объект как измененный
            if (_massSum != value)
            {
                _massSum = value;
                IsModified = true; // Устанавливаем флаг изменения. Он будет использоваться для отслеживания изменений в MassSum
            }
        }
    }

    /// <summary>
    /// Флаг, указывающий, было ли изменено значение MassSum.
    /// Атрибут [NotMapped] говорит Entity Framework не пытаться сохранить это поле в БД.
    /// </summary>
    [NotMapped]
    public bool IsModified { get; private set; } = false;

    /// <summary>
    /// Метод, который сбрасывает состояние "изменен" после сохранения или загрузки данных.
    /// </summary>
    public void ResetModifiedState()
    {
        IsModified = false;
    }

    /// <summary>
    /// Метод для получения CSS-классов инпута на основе состояния объекта.
    /// </summary>
    /// <returns>Строка с CSS-классами</returns>
    [NotMapped]
    public string InputCssClass => IsModified ? "editable-input input-modified" : "editable-input";

}
