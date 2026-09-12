using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using DatabaseLayer;
using Microsoft.EntityFrameworkCore;

namespace DataImport
{
    class Program
    {
        private CollegeFootballEntities entities = null;
        private static ImportSettings settings = new ImportSettings();

        public Program()
        {
            entities = new CollegeFootballEntities();
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }

        public void Run()
        {
            try
            {
                Console.Write("Removing previous data for year {0}...", settings.Year);
                DeleteYearData();
                Console.WriteLine("DONE");

                if (settings.ImportWeekSettings)
                {
                    Console.Write("Importing week settings...");
                    int wsErrors = ImportWeekSettings();
                    if (wsErrors > 0)
                    {
                        Console.WriteLine($"FAILED ({wsErrors} error(s))");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("DONE");
                }

                if (settings.ImportTeams)
                {
                    Console.Write("Creating divisions, conferences, teams, and affiliations...");
                    int teamErrors = ImportTeams();
                    if (teamErrors > 0)
                    {
                        Console.WriteLine($"FAILED ({teamErrors} error(s))");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("DONE");
                }

                if (settings.ImportGames)
                {
                    Console.Write("Importing games...");
                    int gameErrors = ImportGames();
                    if (gameErrors > 0)
                    {
                        Console.WriteLine($"FAILED ({gameErrors} error(s))");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("DONE");
                }

                if (settings.CreateGroups)
                {
                    using var freshContext = new CollegeFootballEntities();
                    TeamGraph tg = new TeamGraph(freshContext);
                    ConferenceGraph cg = new ConferenceGraph(freshContext);
                    DivisionGraph dg = new DivisionGraph(freshContext);

                    Console.Write("Creating team groups...");
                    tg.CreateTeamGroups(settings.Year);
                    Console.WriteLine("DONE");

                    Console.Write("Creating conference groups...");
                    cg.CreateConferenceGroups();
                    Console.WriteLine("DONE");

                    Console.Write("Creating division groups...");
                    dg.CreateDivisionGroups();
                    Console.WriteLine("DONE");
                }

                Console.WriteLine("\nData import complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during data import: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private void DeleteYearData()
        {
            if (!settings.WipeDbOnStart)
            {
                Console.WriteLine("Skipping data wipe (WIPE_DB_ON_START=false)");
                return;
            }

            var games = entities.Games.Where(g => g.Year == settings.Year).ToList();
            entities.Games.RemoveRange(games);

            var weekSettings = entities.WeekSettings.Where(ws => ws.Year == settings.Year).ToList();
            entities.WeekSettings.RemoveRange(weekSettings);

            var teamResults = entities.TeamResults
                .Where(tr => tr.Year == settings.Year).ToList();
            entities.TeamResults.RemoveRange(teamResults);
            var confResults = entities.ConferenceResults
                .Where(cr => cr.Year == settings.Year).ToList();
            entities.ConferenceResults.RemoveRange(confResults);
            var divResults = entities.DivisionResults
                .Where(dr => dr.Year == settings.Year).ToList();
            entities.DivisionResults.RemoveRange(divResults);

            var teamAffs = entities.TeamAffiliations.Where(ta => ta.Year == settings.Year).ToList();
            entities.TeamAffiliations.RemoveRange(teamAffs);
            var confAffs = entities.ConferenceAffiliations.Where(ca => ca.Year == settings.Year).ToList();
            entities.ConferenceAffiliations.RemoveRange(confAffs);

            foreach (var t in entities.Teams) t.Group = null;
            foreach (var c in entities.Conferences) c.Group = null;
            foreach (var d in entities.Divisions) d.Group = null;

            entities.SaveChanges();
        }

        private int ImportWeekSettings()
        {
            if (!File.Exists(settings.WeekSettingsFileName))
            {
                Console.WriteLine($"\nWeek settings file not found: {settings.WeekSettingsFileName}");
                return 1;
            }

            int errors = 0;
            foreach (var line in File.ReadAllLines(settings.WeekSettingsFileName))
            {
                var parts = line.Split(',');
                if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), out int week))
                {
                    Console.WriteLine($"\nInvalid week settings line: {line}");
                    errors++;
                    continue;
                }
                string cutoffDate = parts[1].Trim();
                var existing = entities.WeekSettings.FirstOrDefault(
                    ws => ws.Year == settings.Year && ws.Week == week);
                if (existing != null)
                {
                    existing.CutoffDate = cutoffDate;
                }
                else
                {
                    entities.WeekSettings.Add(new WeekSettings
                    {
                        Year = settings.Year,
                        Week = week,
                        CutoffDate = cutoffDate
                    });
                }
            }
            entities.SaveChanges();
            return errors;
        }

        private int ImportTeams()
        {
            StreamReader reader = new StreamReader(settings.TeamsFileName);
            Division division;
            Conference conference;
            Team team;
            string[] row;
            int errors = 0;

            while (reader.EndOfStream == false)
            {
                string line = reader.ReadLine();
                row = line.Split(',');

                if (row.Length != 3)
                {
                    Console.WriteLine($"\nError in line: {line}");
                    errors++;
                    continue;
                }

                string divisionName = row[0];
                string conferenceName = row[1];
                string teamName = row[2];

                division = entities.Divisions.FirstOrDefault(d => d.Name == divisionName);
                if (division == null)
                {
                    division = new Division { Name = divisionName };
                    entities.Divisions.Add(division);
                    entities.SaveChanges();
                    entities.Entry(division).Reload();
                }

                conference = entities.Conferences.FirstOrDefault(c => c.Name == conferenceName
                                                                    && c.DivisionID == division.ID);
                if (conference == null)
                {
                    conference = new Conference { Name = conferenceName, Division = division };
                    entities.Conferences.Add(conference);
                    entities.SaveChanges();
                    entities.Entry(conference).Reload();
                }

                var confAff = entities.ConferenceAffiliations.FirstOrDefault(
                    ca => ca.ConferenceID == conference.ID && ca.Year == settings.Year);
                if (confAff == null)
                {
                    entities.ConferenceAffiliations.Add(new ConferenceAffiliation
                    {
                        ConferenceID = conference.ID,
                        DivisionID = division.ID,
                        Year = settings.Year
                    });
                    entities.SaveChanges(); // flush so next team in same conf finds it
                }
                else
                {
                    confAff.DivisionID = division.ID;
                }

                team = entities.Teams.FirstOrDefault(t => t.Name == teamName
                                                       && t.ConferenceID == conference.ID);
                if (team == null)
                {
                    team = entities.Teams.FirstOrDefault(t => t.Name == teamName);
                }
                if (team == null)
                {
                    team = new Team { Name = teamName, Conference = conference };
                    entities.Teams.Add(team);
                    entities.SaveChanges();
                    entities.Entry(team).Reload();
                }
                else
                {
                    team.ConferenceID = conference.ID;
                }

                var teamAff = entities.TeamAffiliations.FirstOrDefault(
                    ta => ta.TeamID == team.ID && ta.Year == settings.Year);
                if (teamAff == null)
                {
                    entities.TeamAffiliations.Add(new TeamAffiliation
                    {
                        TeamID = team.ID,
                        ConferenceID = conference.ID,
                        Year = settings.Year
                    });
                }
                else
                {
                    teamAff.ConferenceID = conference.ID;
                }
            }

            entities.SaveChanges();
            return errors;
        }

        private int ImportGames()
        {
            StreamReader reader = new StreamReader(settings.GamesFileName);
            string[] row;
            int errors = 0;

            while (reader.EndOfStream == false)
            {
                string line = reader.ReadLine();
                row = line.Split(',');

                if (row.Length != 6)
                {
                    Console.WriteLine($"\nError in line: {line}");
                    errors++;
                    continue;
                }

                string gameDateString = row[0];
                string awayTeamName = row[1];
                string awayScoreString = row[2];
                string homeTeamName = row[3];
                string homeScoreString = row[4];
                string isNeutralSiteString = row[5];

                Team homeTeam = FindTeam(homeTeamName);
                Team awayTeam = FindTeam(awayTeamName);

                if (homeTeam == null)
                {
                    Console.WriteLine($"\nInvalid home team: {homeTeamName}");
                    errors++;
                    continue;
                }
                if (awayTeam == null)
                {
                    Console.WriteLine($"\nInvalid away team: {awayTeamName}");
                    errors++;
                    continue;
                }

                int homeScore, awayScore;
                bool isNeutralSite;
                DateTime gameDate;

                try { homeScore = Convert.ToInt32(homeScoreString); }
                catch (FormatException) { Console.WriteLine($"\nInvalid score in line: {line}"); errors++; continue; }

                try { awayScore = Convert.ToInt32(awayScoreString); }
                catch (FormatException) { Console.WriteLine($"\nInvalid score in line: {line}"); errors++; continue; }

                if (isNeutralSiteString.Equals("true")) isNeutralSite = true;
                else if (isNeutralSiteString.Equals("false")) isNeutralSite = false;
                else { Console.WriteLine($"\nInvalid isNeutralSite in line: {line}"); errors++; continue; }

                try { gameDate = Convert.ToDateTime(gameDateString); }
                catch (Exception) { Console.WriteLine($"\nInvalid date in line: {line}"); errors++; continue; }

                if (homeScore == 0 && awayScore == 0)
                    continue; // unplayed game — skip

                var game = new Game
                {
                    HomeTeam = homeTeam,
                    HomeScore = homeScore,
                    AwayTeam = awayTeam,
                    AwayScore = awayScore,
                    IsNeutralSite = isNeutralSite,
                    Date = gameDate,
                    Year = settings.Year
                };
                entities.Games.Add(game);
            }

            entities.SaveChanges();
            return errors;
        }

        private Team FindTeam(string name)
        {
            var team = entities.Teams.FirstOrDefault(t => t.Name == name);
            if (team != null) return team;
            var nameLower = name.ToLower();
            team = entities.Teams.AsEnumerable().FirstOrDefault(t => t.Name.ToLower() == nameLower);
            if (team != null) return team;
            var normalized = Normalize(nameLower);
            return entities.Teams.AsEnumerable().FirstOrDefault(t => Normalize(t.Name.ToLower()) == normalized);
        }

        private static string Normalize(string s) =>
            Regex.Replace(Regex.Replace(s, @"[^a-z0-9 ]", " "), @"\s+", " ").Trim();
    }
}
