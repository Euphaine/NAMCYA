using EventScoringSystem.Data;
using EventScoringSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EventScoringSystem.Services
{
    public class TabulationService
    {
        private readonly AppDbContext _db;

        public TabulationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<ContestantResultModel>> ComputeEventResultsAsync(int eventId)
        {
            var contestants = await _db.Contestants.Where(c => c.EventId == eventId).OrderBy(c => c.ContestantNumber).ToListAsync();
            var judges = await _db.Judges.Where(j => j.EventId == eventId).OrderBy(j => j.Id).ToListAsync();
            var criteria = await _db.Criteria.Where(cr => cr.EventId == eventId).ToListAsync();
            var allScores = await _db.Scores.Where(s => s.EventId == eventId).ToListAsync();

            var results = new List<ContestantResultModel>();
            foreach (var contestant in contestants)
            {
                var res = new ContestantResultModel { Contestant = contestant };
                
                decimal totalJudgeScoreSum = 0;
                int activeJudgesCount = 0;

                foreach (var judge in judges)
                {
                    var judgeContestantScores = allScores.Where(s => s.JudgeId == judge.Id && s.ContestantId == contestant.Id).ToList();
                    decimal judgeSubtotal = 0;
                    
                    foreach (var score in judgeContestantScores)
                    {
                        var crit = criteria.FirstOrDefault(c => c.Id == score.CriterionId);
                        if (crit != null) 
                        {
                            judgeSubtotal += score.ScoreValue;
                        }
                    }

                    res.JudgeDetails.Add(new JudgeDetailModel { JudgeId = judge.Id, Score = judgeSubtotal, Rank = 0 });

                    if (judgeContestantScores.Any())
                    {
                        totalJudgeScoreSum += judgeSubtotal;
                        activeJudgesCount++;
                    }
                }

                res.FinalScore = activeJudgesCount > 0 ? (totalJudgeScoreSum / activeJudgesCount) : 0;
                results.Add(res);
            }

            // Fractional ranking per judge
            foreach (var judge in judges)
            {
                var sortedByJudge = results.Select(r => new { Result = r, Detail = r.JudgeDetails.First(jd => jd.JudgeId == judge.Id) })
                    .OrderByDescending(x => x.Detail.Score).ToList();

                int i = 0;
                while (i < sortedByJudge.Count)
                {
                    int j = i;
                    while (j < sortedByJudge.Count && sortedByJudge[j].Detail.Score == sortedByJudge[i].Detail.Score) j++;
                    
                    decimal sumOfRanks = 0;
                    for (int k = i + 1; k <= j; k++) sumOfRanks += k;
                    decimal averageRank = sumOfRanks / (j - i);
                    
                    for (int k = i; k < j; k++) sortedByJudge[k].Detail.Rank = averageRank;
                    i = j;
                }
            }

            // Sum up ranks across judges
            foreach (var res in results) res.RankSum = res.JudgeDetails.Sum(jd => jd.Rank);
            
            // Overall ranking sorted by SumRank (ascending) then FinalScore (descending)
            var sortedByRankSum = results.OrderBy(r => r.RankSum).ThenByDescending(r => r.FinalScore).ToList();
            int rankIdx = 0;
            while (rankIdx < sortedByRankSum.Count)
            {
                int j = rankIdx;
                // Group together ONLY if BOTH RankSum and FinalScore are completely identical (literal tie)
                while (j < sortedByRankSum.Count && 
                       sortedByRankSum[j].RankSum == sortedByRankSum[rankIdx].RankSum && 
                       sortedByRankSum[j].FinalScore == sortedByRankSum[rankIdx].FinalScore) 
                {
                    j++;
                }

                // Calculate fractional rank for the tied group
                decimal sumOfRanks = 0;
                for (int k = rankIdx + 1; k <= j; k++) sumOfRanks += k;
                decimal averageRank = sumOfRanks / (j - rankIdx);

                for (int k = rankIdx; k < j; k++) 
                {
                    sortedByRankSum[k].OverallRank = averageRank;
                }
                rankIdx = j;
            }

            return results.OrderBy(r => r.RankSum).ThenBy(r => r.Contestant.ContestantNumber).ToList();
        }
    }

    public class JudgeDetailModel
    {
        public int JudgeId { get; set; }
        public decimal Score { get; set; }
        public decimal Rank { get; set; }
    }

    public class ContestantResultModel
    {
        public Contestant Contestant { get; set; } = default!;
        public List<JudgeDetailModel> JudgeDetails { get; set; } = new();
        public decimal RankSum { get; set; }
        public decimal FinalScore { get; set; }
        public decimal OverallRank { get; set; } // Changed from int to decimal to support fractional ranks
    }
}