using CsvHelper.Configuration.Attributes;

namespace MySets.Import;

public sealed class SetCsvRow
{
    [Name("set_num")]
    public string SetNum { get; set; } = string.Empty;

    [Name("name")]
    public string Name { get; set; } = string.Empty;

    [Name("year")]
    public int Year { get; set; }

    [Name("theme_id")]
    public int ThemeId { get; set; }

    [Name("num_parts")]
    public int NumParts { get; set; }

    [Name("img_url")]
    public string? ImgUrl { get; set; }
}
