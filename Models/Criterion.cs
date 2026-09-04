namespace EventScoringSystem.Models;

public class Criterion
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; } // e.g., 40.0 for 40%
}