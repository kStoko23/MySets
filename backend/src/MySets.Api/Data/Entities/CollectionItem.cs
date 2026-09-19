namespace MySets.Api.Data.Entities;

public class CollectionItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string SetId { get; set; } = string.Empty;
    public Set Set { get; set; } = null!;
    public DateTime? BuiltAt { get; set; }
    public bool IsPublic { get; set; } = true;
}
