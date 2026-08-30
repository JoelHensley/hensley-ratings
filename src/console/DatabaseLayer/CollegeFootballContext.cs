using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer
{
    public partial class CollegeFootballEntities : DbContext
    {
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Conference> Conferences { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<TeamResult> TeamResults { get; set; }
        public DbSet<ConferenceResult> ConferenceResults { get; set; }
        public DbSet<DivisionResult> DivisionResults { get; set; }

        public CollegeFootballEntities()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlite("Data Source=collegefootball.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>()
                .Ignore(t => t.Games)
                .Ignore(t => t.Opponents);

            modelBuilder.Entity<TeamResult>()
                .HasOne(tr => tr.Team)
                .WithOne(t => t.TeamResult)
                .HasForeignKey<TeamResult>(tr => tr.TeamID);

            modelBuilder.Entity<ConferenceResult>()
                .HasOne(cr => cr.Conference)
                .WithOne(c => c.ConferenceResult)
                .HasForeignKey<ConferenceResult>(cr => cr.ConferenceID);

            modelBuilder.Entity<DivisionResult>()
                .HasOne(dr => dr.Division)
                .WithOne(d => d.DivisionResult)
                .HasForeignKey<DivisionResult>(dr => dr.DivisionID);

            modelBuilder.Entity<Game>()
                .HasOne(g => g.HomeTeam)
                .WithMany(t => t.HomeGames)
                .HasForeignKey(g => g.HomeTeamID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Game>()
                .HasOne(g => g.AwayTeam)
                .WithMany(t => t.AwayGames)
                .HasForeignKey(g => g.AwayTeamID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
