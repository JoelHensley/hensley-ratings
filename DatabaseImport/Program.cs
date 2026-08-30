/* Program.cs
 * Joel Hensley
 * January 13, 2010
 * This class is used to remove any previous data from the database,
 * import the teams, conferences, divisions, and games into the
 * database, and finally create the different groups for the teams,
 * conferences, and divisions.
 */
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public Program()
        {
            entities = new CollegeFootballEntities();
        }

        /// <summary>
        /// Calls run
        /// </summary>
        /// <param name="args">No args are required</param>
        static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }

        /// <summary>
        /// Removes any previous data in the database, import the teams, conferences
        /// and divisions, imports the games, and the creates the groups for the
        /// teams, conferences, and divisions.
        /// </summary>
        public void Run()
        {
            try
            {
                if (settings.DeleteExistingData)
                {
                    Console.Write("Removing previous data...");
                    DeleteExistingData();
                    Console.WriteLine("DONE");
                }

                if (settings.ImportTeams)
                {
                    Console.Write("Creating divisions, conferences, and teams...");
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
                    // Use a fresh context so all entities are materialized as lazy-loading proxies
                    // (entities created with 'new' in ImportTeams/ImportGames are plain objects without ILazyLoader).
                    using var freshContext = new CollegeFootballEntities();
                    TeamGraph tg = new TeamGraph(freshContext);
                    ConferenceGraph cg = new ConferenceGraph(freshContext);
                    DivisionGraph dg = new DivisionGraph(freshContext);

                    Console.Write("Creating team groups...");
                    tg.CreateTeamGroups();
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

        /// <summary>
        /// Removes all data in the database.
        /// </summary>
        private void DeleteExistingData()
        {
            entities.DeleteAllRows();
        }

        /// <summary>
        /// Imports the teams, conferences, and divisions.
        /// </summary>
        /// <remarks>
        /// The comma-delimited file must have the following
        /// format: divisionName,conferenceName,teamName
        /// </remarks>
        private int ImportTeams()
        {
            StreamReader reader = new StreamReader(settings.TeamsFileName);
            Division division;
            Conference conference;
            Team team;
            string[] row;
            string divisionName;
            string conferenceName;
            string teamName;
            int errors = 0;

            while (reader.EndOfStream == false)
            {
                String line = reader.ReadLine();

                // CSV Format: <Division name>,<Conference name>,<Team name>
                row = line.Split(',');

                if (row.Length != 3)
                {
                    Console.WriteLine(String.Format("Error in line: {0}", line));
                    errors++;
                    continue;
                }

                divisionName = row[0];
                conferenceName = row[1];
                teamName = row[2];

                division = entities.Divisions.FirstOrDefault(d => d.Name ==
                                                divisionName);

                if (division == null)
                {
                    // Division does not exist, so create one
                    division = new Division();
                    division.Name = divisionName;
                    entities.Divisions.Add(division);
                    entities.SaveChanges();
                    entities.Entry(division).Reload();
                }

                conference = entities.Conferences.FirstOrDefault(c => c.Name ==
                                conferenceName && c.DivisionID == division.ID);

                if (conference == null)
                {
                    // Conference does not exist, so create one
                    conference = new Conference();
                    conference.Name = conferenceName;
                    conference.Division = division;
                    entities.Conferences.Add(conference);
                    entities.SaveChanges();
                    entities.Entry(conference).Reload();
                }

                team = entities.Teams.FirstOrDefault(t => t.Name == teamName
                                            && t.ConferenceID == conference.ID);

                if (team == null)
                {
                    // Team does not exist, so create one
                    team = new Team();
                    team.Name = teamName;
                    team.Conference = conference;
                    entities.Teams.Add(team);
                }
            }

            entities.SaveChanges();
            return errors;
        }

        /// <summary>
        /// Imports the games.
        /// </summary>
        /// <remarks>
        /// The comma-delimited file must have the following format:
        /// GameDate,AwayTeamName,AwayTeamScore,
        /// HomeTeamName,HomeTeamScore,IsNeutralSite
        /// The value IsNeutralSite must be either "true" or "false"
        /// </remarks>
        private int ImportGames()
        {
            StreamReader reader = new StreamReader(settings.GamesFileName);
            Game game;
            Team homeTeam;
            Team awayTeam;
            string[] row;
            string homeTeamName;
            string awayTeamName;
            string homeScoreString;
            string awayScoreString;
            string gameDateString;
            string isNeutralSiteString;
            DateTime gameDate;
            int homeScore;
            int awayScore;
            bool isNeutralSite;
            int errors = 0;

            while (reader.EndOfStream == false)
            {
                String line = reader.ReadLine();

                // CSV Format: <Game date>,<Away team name>,<Away team score>,
                // <Home team name>,<Home team score>,<Is Neutral Site>
                row = line.Split(',');

                if (row.Length != 6)
                {
                    Console.WriteLine(String.Format("Error in line: {0}", line));
                    errors++;
                    continue;
                }

                gameDateString = row[0];
                awayTeamName = row[1];
                awayScoreString = row[2];
                homeTeamName = row[3];
                homeScoreString = row[4];
                isNeutralSiteString = row[5];

                homeTeam = FindTeam(homeTeamName);
                awayTeam = FindTeam(awayTeamName);

                if (homeTeam == null)
                {
                    Console.WriteLine(String.Format("Invalid home team: {0}",
                                                    homeTeamName));
                    errors++;
                    continue;
                }

                if (awayTeam == null)
                {
                    Console.WriteLine(String.Format("Invalid away team: {0}",
                                                    awayTeamName));
                    errors++;
                    continue;
                }

                try
                {
                    homeScore = Convert.ToInt32(homeScoreString);
                    awayScore = Convert.ToInt32(awayScoreString);
                }
                catch (FormatException)
                {
                    Console.WriteLine(String.Format("Invalid score in line: {0}",
                                                    line));
                    errors++;
                    continue;
                }

                if (isNeutralSiteString.Equals("true"))
                {
                    isNeutralSite = true;
                }
                else if (isNeutralSiteString.Equals("false"))
                {
                    isNeutralSite = false;
                }
                else
                {
                    Console.WriteLine(String.Format("Invalid isNeutralSite in line: " +
                                                    "{0}", line));
                    errors++;
                    continue;
                }

                try
                {
                    gameDate = Convert.ToDateTime(gameDateString);
                }
                catch (Exception)
                {
                    Console.WriteLine(String.Format("Invalid date in line: {0}",
                                                    line));
                    errors++;
                    continue;
                }

                game = new Game();
                game.HomeTeam = homeTeam;
                game.HomeScore = homeScore;
                game.AwayTeam = awayTeam;
                game.AwayScore = awayScore;
                game.IsNeutralSite = isNeutralSite;
                game.Date = gameDate;

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
            team = entities.Teams.AsEnumerable().FirstOrDefault(t => Normalize(t.Name.ToLower()) == normalized);
            return team;
        }

        private static string Normalize(string s) =>
            Regex.Replace(Regex.Replace(s, @"[^a-z0-9 ]", " "), @"\s+", " ").Trim();
    }
}
