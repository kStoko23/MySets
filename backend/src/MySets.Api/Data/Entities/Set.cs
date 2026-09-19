namespace MySets.Api.Data.Entities;

public class Set
{
    /// <summary>Rebrickable's set_num (e.g. "7922-1"), used as-is as the primary key.</summary>
    public string RebrickableSetNum { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Theme { get; set; }
    public int Year { get; set; }
    public int PieceCount { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
