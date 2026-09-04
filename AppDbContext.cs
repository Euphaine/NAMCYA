using Microsoft.EntityFrameworkCore;
using EventScoringSystem.Models;

namespace EventScoringSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EventItem> Events => Set<EventItem>();
    public DbSet<Contestant> Contestants => Set<Contestant>();
    public DbSet<Judge> Judges => Set<Judge>();
    public DbSet<Criterion> Criteria => Set<Criterion>();
    public DbSet<Score> Scores => Set<Score>();
}