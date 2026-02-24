namespace ParserLib.Gorzdrav.Models;

/// <summary>
/// Представляет информацию о препарате
/// </summary>
public class Drug
{
    /// <summary>
    /// Рецептурность препарата (пустая строка - без рецепта)
    /// </summary>
    public string PrescriptionStatus { get; set; } = string.Empty;

    /// <summary>
    /// Ссылка на полноразмерное изображение препарата
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Название препарата
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Производитель препарата
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Активное вещество (действующее вещество)
    /// </summary>
    public string ActiveSubstance { get; set; } = string.Empty;

    /// <summary>
    /// Цена препарата
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Старая цена препарата (если есть, иначе null)
    /// </summary>
    public decimal? OldPrice { get; set; }

    /// <summary>
    /// Ссылка на страницу препарата
    /// </summary>
    public string DrugUrl { get; set; } = string.Empty;
}