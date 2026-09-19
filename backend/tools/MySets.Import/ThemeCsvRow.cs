using CsvHelper.Configuration.Attributes;

namespace MySets.Import;

public sealed class ThemeCsvRow
{
    [Name("id")]
    public int Id { get; set; }

    [Name("name")]
    public string Name { get; set; } = string.Empty;

    [Name("parent_id")]
    public int? ParentId { get; set; }
}
