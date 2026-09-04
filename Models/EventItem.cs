namespace EventScoringSystem.Models;

public class EventItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsFinalized { get; set; } = false; // Add this if not already present
    public DateTime EventDate { get; set; } = DateTime.Now;
    public string Description { get; set; } = string.Empty;

    public ICollection<Judge> Judges { get; set; } = new List<Judge>();
    public ICollection<Contestant> Contestants { get; set; } = new List<Contestant>();
    public ICollection<Criterion> Criteria { get; set; } = new List<Criterion>();
}