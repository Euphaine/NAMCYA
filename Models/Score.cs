namespace EventScoringSystem.Models
{
    public class Score
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int JudgeId { get; set; }
        public int ContestantId { get; set; }
        public int CriterionId { get; set; }
        public decimal ScoreValue { get; set; }
    }
}