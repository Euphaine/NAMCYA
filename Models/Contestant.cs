namespace EventScoringSystem.Models;

public class Contestant
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Region { get; set; }
    public int ContestantNumber { get; set; }
}