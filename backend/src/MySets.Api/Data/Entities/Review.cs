namespace MySets.Api.Data.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string SetId { get; set; } = string.Empty;
    public Set Set { get; set; } = null!;
    public int Rating { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
