using System.ComponentModel.DataAnnotations;

namespace AddCorrectTable.Models;

public class Material
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Поле 'Код' обязательно для заполнения")]
    [StringLength(20, ErrorMessage = "Длина кода не должна превышать 20 символов")]
    public string Kodn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Поле 'Наименование' обязательно для заполнения")]
    [StringLength(100, ErrorMessage = "Длина наименования не должна превышать 100 символов")]
    public string Name { get; set; } = string.Empty;

}
