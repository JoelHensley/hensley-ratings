using System;
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
        public DbSet<WeekSettings> WeekSettings { get; set; }
        public DbSet<TeamAffiliation> TeamAffiliations { get; set; }
        public DbSet<ConferenceAffiliation> ConferenceAffiliations { get; set; }

        public CollegeFootballEntities()
        {
            Database.EnsureCreated();
            EnsureSchemaUpgrades();
        }

        public CollegeFootballEntities(DbContextOptions<CollegeFootballEntities> options)
            : base(options)
        {
            Database.EnsureCreated();
            EnsureSchemaUpgrades();
        }

        private void EnsureSchemaUpgrades()
        {
            // Add columns introduced after initial schema creation.
            // ALTER TABLE ADD COLUMN is idempotent in SQLite when wrapped in try/catch.
            try { Database.ExecuteSqlRaw("ALTER TABLE WeekSettings ADD COLUMN ComputedGameCount INTEGER"); }
            catch { }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured) return;
            var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "collegefootball.db";
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>()
                .Ignore(t => t.Games)
                .Ignore(t => t.Opponents);

            // TeamResult: many per team (year+week keyed)
            modelBuilder.Entity<TeamResult>()
                .HasOne(tr => tr.Team)
                .WithMany()
                .HasForeignKey(tr => tr.TeamID);

            modelBuilder.Entity<TeamResult>()
                .HasIndex(tr => new { tr.TeamID, tr.Year, tr.Week })
                .IsUnique();

            // ConferenceResult: many per conference (year+week keyed)
            modelBuilder.Entity<ConferenceResult>()
                .HasOne(cr => cr.Conference)
                .WithMany()
                .HasForeignKey(cr => cr.ConferenceID);

            modelBuilder.Entity<ConferenceResult>()
                .HasIndex(cr => new { cr.ConferenceID, cr.Year, cr.Week })
                .IsUnique();

            // DivisionResult: many per division (year+week keyed)
            modelBuilder.Entity<DivisionResult>()
                .HasOne(dr => dr.Division)
                .WithMany()
                .HasForeignKey(dr => dr.DivisionID);

            modelBuilder.Entity<DivisionResult>()
                .HasIndex(dr => new { dr.DivisionID, dr.Year, dr.Week })
                .IsUnique();

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

            // TeamAffiliation: year-keyed team→conference mapping
            modelBuilder.Entity<TeamAffiliation>()
                .HasIndex(ta => new { ta.TeamID, ta.Year })
                .IsUnique();

            modelBuilder.Entity<TeamAffiliation>()
                .HasOne(ta => ta.Team)
                .WithMany(t => t.TeamAffiliations)
                .HasForeignKey(ta => ta.TeamID);

            modelBuilder.Entity<TeamAffiliation>()
                .HasOne(ta => ta.Conference)
                .WithMany()
                .HasForeignKey(ta => ta.ConferenceID);

            // ConferenceAffiliation: year-keyed conference→division mapping
            modelBuilder.Entity<ConferenceAffiliation>()
                .HasIndex(ca => new { ca.ConferenceID, ca.Year })
                .IsUnique();

            modelBuilder.Entity<ConferenceAffiliation>()
                .HasOne(ca => ca.Conference)
                .WithMany(c => c.ConferenceAffiliations)
                .HasForeignKey(ca => ca.ConferenceID);

            modelBuilder.Entity<ConferenceAffiliation>()
                .HasOne(ca => ca.Division)
                .WithMany()
                .HasForeignKey(ca => ca.DivisionID);

            // WeekSettings: unique per year+week
            modelBuilder.Entity<WeekSettings>()
                .HasIndex(ws => new { ws.Year, ws.Week })
                .IsUnique();
        }
    }
}
