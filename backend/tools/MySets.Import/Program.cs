using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using MySets.Api.Data;
using MySets.Import;
using Set = MySets.Api.Data.Entities.Set;

var dataDirectory = args[0];
var setsPath = Path.Combine(dataDirectory, "sets.csv", "sets.csv");
var themesPath = Path.Combine(dataDirectory, "themes.csv", "themes.csv");

if (!File.Exists(setsPath))
{
    Console.Error.WriteLine($"Nie znaleziono pliku: {setsPath}");
    return 1;
}

if (!File.Exists(themesPath))
{
    Console.Error.WriteLine($"Nie znaleziono pliku: {themesPath}");
    return 1;
}

var connectionString = Environment.GetEnvironmentVariable("MYSETS_IMPORT_CONNECTION");

var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .Options;

await using var db = new AppDbContext(dbOptions);

Console.WriteLine("Wczytywanie themes.csv...");
var themeNamesById = ReadRecords<ThemeCsvRow>(themesPath).ToDictionary(t => t.Id, t => t.Name);
Console.WriteLine($"  {themeNamesById.Count} tematów.");

Console.WriteLine("Wczytywanie sets.csv...");
var rows = ReadRecords<SetCsvRow>(setsPath).ToList();
Console.WriteLine($"  {rows.Count} setów do przetworzenia.");

Console.WriteLine("Pobieranie istniejącego katalogu setów z bazy...");
var existingSets = await db.Sets.ToDictionaryAsync(s => s.RebrickableSetNum);

var inserted = 0;
var updated = 0;
var processed = 0;
const int batchSize = 1000;

foreach (var row in rows)
{
    themeNamesById.TryGetValue(row.ThemeId, out var themeName);
    var imageUrl = string.IsNullOrWhiteSpace(row.ImgUrl) ? null : row.ImgUrl;

    if (existingSets.TryGetValue(row.SetNum, out var set))
    {
        set.Name = row.Name;
        set.Theme = themeName;
        set.Year = row.Year;
        set.PieceCount = row.NumParts;
        set.ImageUrl = imageUrl;
        updated++;
    }
    else
    {
        db.Sets.Add(new Set
        {
            RebrickableSetNum = row.SetNum,
            Name = row.Name,
            Theme = themeName,
            Year = row.Year,
            PieceCount = row.NumParts,
            ImageUrl = imageUrl,
        });
        inserted++;
    }

    processed++;
    if (processed % batchSize == 0)
    {
        await db.SaveChangesAsync();
        Console.WriteLine($"  ...{processed}/{rows.Count} (insert: {inserted}, update: {updated})");
    }
}

await db.SaveChangesAsync();
Console.WriteLine($"Gotowe. Przetworzono {processed} setów (insert: {inserted}, update: {updated}).");
return 0;

static List<T> ReadRecords<T>(string path)
{
    using var reader = new StreamReader(path);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    return csv.GetRecords<T>().ToList();
}
