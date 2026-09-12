/* Program.cs
 * Joel Hensley
 * January 16, 2010
 * This class is used to call the different rating systems, write the results
 * to CSV files, write the results to the database, and then output
 * the progress to the console.
 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DatabaseLayer;

namespace RatingSystem
{
    class Program
    {
        private RatingSettings settings;
        private CollegeFootballEntities entities;
        private Dictionary<int, double> stdRatingVector = null;
        private Dictionary<int, double> hfaRatingVector = null;
        private Dictionary<int, double> mpdRatingVector = null;
        private Dictionary<int, double> hensleyRatingVector = null;
        private Dictionary<int, double> stdSSVector = null;
        private Dictionary<int, double> hfaSSVector = null;
        private Dictionary<int, double> mpdSSVector = null;
        private Dictionary<int, double> hensleySSVector = null;
        private double homeFieldAdvantage;
        private const int RoundDecimals = 3;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Program()
        {
            settings = new RatingSettings();
            entities = new CollegeFootballEntities();
            homeFieldAdvantage = 0;
        }

        /// <summary>
        /// Calls run
        /// </summary>
        /// <param name="args">No args are requried</param>
        static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();

        }

        /// <summary>
        /// Loops through each group in the teams, conferences, and divisions
        /// and computes the ratings specified in the settings file.
        /// </summary>
        public void Run()
        {
            int count;
            List<int?> groups = null;

            if (settings.ComputeTeamRatings)
            {
                groups = entities.Teams.Where(t => t.Group != null).Select(t =>
                                        t.Group).Distinct().ToList();

                foreach (int group in groups)
                {
                    count = entities.Teams.Where(t => t.Group == group).Count();

                    if (count >= settings.MinGroupSize)
                    {
                        // There must be at least 2 teams in the group to calculate
                        // ratings
                        ComputeTeamRatings(group);
                    }
                }
            }

            if (settings.ComputeConferenceRatings)
            {
                groups = entities.Conferences.Where(c => c.Group != null).Select(
                                        c => c.Group).Distinct().ToList();

                foreach (int group in groups)
                {
                    count = entities.Conferences.Where(c => c.Group ==
                                                        group).Count();

                    if (count >= settings.MinGroupSize)
                    {
                        // There must be at least 2 conferences in the group to
                        // calculate ratings
                        ComputeConferenceRatings(group);
                    }
                }
            }

            if (settings.ComputeDivisionRatings)
            {
                groups = entities.Divisions.Where(d => d.Group != null).Select(d =>
                                        d.Group).Distinct().ToList();

                foreach (int group in groups)
                {
                    count = entities.Divisions.Where(d => d.Group == group).Count();

                    if (count >= settings.MinGroupSize)
                    {
                        // There must be at least 2 divisions in the group to
                        // calculate ratings
                        ComputeDivisionRatings(group);
                    }
                }
            }
        }

        /// <summary>
        /// Computes the rating types for teams specified in the settings file.
        /// </summary>
        /// <param name="group">The group of teams to process</param>
        public void ComputeTeamRatings(int group)
        {
            double[][] matrix;
            stdSSVector = null; hfaSSVector = null; mpdSSVector = null; hensleySSVector = null;

            if (settings.ComputeStandardRatings
                || settings.ComputeHomefieldAdvantageRatings
                || settings.ComputeMaxPointDifferentialRatings
                || settings.ComputeHensleyRatings)
            {
                Console.WriteLine("Computing Team Ratings");
                Console.WriteLine("---------------------");
            }

            if (settings.ComputeStandardRatings)
            {
                StandardRating stdRating = new StandardRating(entities, group);
                Console.WriteLine("\n{0} for group {1}", StandardRating.RatingName,
                                group);
                Console.Write("Creating team matrix...");
                matrix = stdRating.CreateTeamMatrix();
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                stdRatingVector = stdRating.GetRatings(matrix, RatingObject.Team);
                stdSSVector = ComputeTeamScheduleStrength(group, stdRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeHomefieldAdvantageRatings)
            {
                HomeFieldAdvantageRating hfaRating = new
                                HomeFieldAdvantageRating(entities, group);
                Console.WriteLine("\n{0} for group {1}",
                                HomeFieldAdvantageRating.RatingName, group);
                Console.Write("Creating team matrix...");
                matrix = hfaRating.CreateTeamMatrix();
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                hfaRatingVector = hfaRating.GetRatings(matrix, RatingObject.Team);
                homeFieldAdvantage = hfaRating.HomeFieldAdvantage;
                hfaSSVector = ComputeTeamScheduleStrength(group, hfaRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeMaxPointDifferentialRatings)
            {
                MaxPointDifferentialRating mpdRating = new
                                MaxPointDifferentialRating(entities, group,
                                settings.MaxPointDifferential);
                Console.WriteLine("\n{0} for group {1}",
                                MaxPointDifferentialRating.RatingName, group);
                Console.Write("Creating team matrix...");
                matrix = mpdRating.CreateTeamMatrix();
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                mpdRatingVector = mpdRating.GetRatings(matrix, RatingObject.Team);
                mpdSSVector = ComputeTeamScheduleStrength(group, mpdRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeHensleyRatings)
            {
                HensleyRating hensleyRating = new HensleyRating(entities, group,
                                settings.LowerBounds, settings.UpperBounds);
                Console.WriteLine("\n{0} for group {1}", HensleyRating.RatingName,
                                group);
                Console.Write("Creating team matrix...");
                matrix = hensleyRating.CreateTeamMatrix();
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                hensleyRatingVector = hensleyRating.GetRatings(matrix,
                                                RatingObject.Team);
                hensleySSVector = ComputeTeamScheduleStrength(group, hensleyRatingVector);
                Console.WriteLine("DONE");
            }

            OutputResults(group, RatingObject.Team, null);
        }

        /// <summary>
        /// Computes the rating types for conferences specified in the settings
        /// file.
        /// </summary>
        /// <param name="group">The group of conferences to process</param>
        public void ComputeConferenceRatings(int group)
        {
            double[][] matrix;
            IEnumerable<Game> interConferenceGames;
            stdSSVector = null; hfaSSVector = null; mpdSSVector = null; hensleySSVector = null;

            interConferenceGames = entities.GetInterConferenceGames(group, settings.Year);

            if (settings.ComputeStandardRatings
                || settings.ComputeHomefieldAdvantageRatings
                || settings.ComputeMaxPointDifferentialRatings
                || settings.ComputeHensleyRatings)
            {
                Console.WriteLine("\nComputing Conference Ratings");
                Console.WriteLine("---------------------------");
            }

            if (settings.ComputeStandardRatings)
            {
                StandardRating stdRating = new StandardRating(entities, group);
                Console.WriteLine("\n{0} for group {1}", StandardRating.RatingName,
                                group);
                Console.Write("Creating conference matrix...");
                matrix = stdRating.CreateConferenceMatrix(interConferenceGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                stdRatingVector = stdRating.GetRatings(matrix,
                                                RatingObject.Conference);
                stdSSVector = ComputeConferenceScheduleStrength(interConferenceGames, group, stdRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeHomefieldAdvantageRatings)
            {
                HomeFieldAdvantageRating hfaRating = new
                                HomeFieldAdvantageRating(entities, group);
                Console.WriteLine("\n{0} for group {1}",
                                HomeFieldAdvantageRating.RatingName, group);
                Console.Write("Creating conference matrix...");
                matrix = hfaRating.CreateConferenceMatrix(interConferenceGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                hfaRatingVector = hfaRating.GetRatings(matrix,
                                                RatingObject.Conference);
                homeFieldAdvantage = hfaRating.HomeFieldAdvantage;
                hfaSSVector = ComputeConferenceScheduleStrength(interConferenceGames, group, hfaRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeMaxPointDifferentialRatings)
            {
                MaxPointDifferentialRating mpdRating = new
                                MaxPointDifferentialRating(entities, group,
                                settings.MaxPointDifferential);
                Console.WriteLine("\n{0} for group {1}",
                                MaxPointDifferentialRating.RatingName, group);
                Console.Write("Creating conference matrix...");
                matrix = mpdRating.CreateConferenceMatrix(interConferenceGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                mpdRatingVector = mpdRating.GetRatings(matrix,
                                                RatingObject.Conference);
                mpdSSVector = ComputeConferenceScheduleStrength(interConferenceGames, group, mpdRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeHensleyRatings)
            {
                HensleyRating hensleyRating = new HensleyRating(entities, group,
                                settings.LowerBounds, settings.UpperBounds);
                Console.WriteLine("\n{0} for group {1}", HensleyRating.RatingName,
                                group);
                Console.Write("Creating conference matrix...");
                matrix = hensleyRating.CreateConferenceMatrix(interConferenceGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                hensleyRatingVector = hensleyRating.GetRatings(matrix,
                                                RatingObject.Conference);
                hensleySSVector = ComputeConferenceScheduleStrength(interConferenceGames, group, hensleyRatingVector);
                Console.WriteLine("DONE");
            }

            OutputResults(group, RatingObject.Conference, interConferenceGames);
        }

        /// <summary>
        /// Computes the rating types for divisions specified in the settings file.
        /// </summary>
        /// <param name="group">The group of divisions to process</param>
        public void ComputeDivisionRatings(int group)
        {
            double[][] matrix;
            IEnumerable<Game> interDivisionGames;
            stdSSVector = null; hfaSSVector = null; mpdSSVector = null; hensleySSVector = null;

            interDivisionGames = entities.GetInterDivisionGames(group, settings.Year);

            if (settings.ComputeStandardRatings
                || settings.ComputeHomefieldAdvantageRatings
                || settings.ComputeMaxPointDifferentialRatings
                || settings.ComputeHensleyRatings)
            {
                Console.WriteLine("\nComputing Division Ratings");
                Console.WriteLine("---------------------------");
            }

            if (settings.ComputeStandardRatings)
            {
                StandardRating stdRating = new StandardRating(entities, group);
                Console.WriteLine("\n{0} for group {1}", StandardRating.RatingName,
                                group);
                Console.Write("Creating division matrix...");
                matrix = stdRating.CreateDivisionMatrix(interDivisionGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                stdRatingVector = stdRating.GetRatings(matrix,
                                                RatingObject.Division);
                stdSSVector = ComputeDivisionScheduleStrength(interDivisionGames, group, stdRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeHomefieldAdvantageRatings)
            {
                HomeFieldAdvantageRating hfaRating = new
                                HomeFieldAdvantageRating(entities, group);
                Console.WriteLine("\n{0} for group {1}",
                                HomeFieldAdvantageRating.RatingName, group);
                Console.Write("Creating division matrix...");
                matrix = hfaRating.CreateDivisionMatrix(interDivisionGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                hfaRatingVector = hfaRating.GetRatings(matrix,
                                                RatingObject.Division);
                homeFieldAdvantage = hfaRating.HomeFieldAdvantage;
                hfaSSVector = ComputeDivisionScheduleStrength(interDivisionGames, group, hfaRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeMaxPointDifferentialRatings)
            {
                MaxPointDifferentialRating mpdRating = new
                                MaxPointDifferentialRating(entities, group,
                                settings.MaxPointDifferential);
                Console.WriteLine("\n{0} for group {1}",
                                MaxPointDifferentialRating.RatingName, group);
                Console.Write("Creating division matrix...");
                matrix = mpdRating.CreateDivisionMatrix(interDivisionGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                mpdRatingVector = mpdRating.GetRatings(matrix,
                                                RatingObject.Division);
                mpdSSVector = ComputeDivisionScheduleStrength(interDivisionGames, group, mpdRatingVector);
                Console.WriteLine("DONE");
            }

            if (settings.ComputeHensleyRatings)
            {
                HensleyRating hensleyRating = new HensleyRating(entities, group,
                                settings.LowerBounds, settings.UpperBounds);
                Console.WriteLine("\n{0} for group {1}", HensleyRating.RatingName,
                                group);
                Console.Write("Creating division matrix...");
                matrix = hensleyRating.CreateDivisionMatrix(interDivisionGames);
                Console.WriteLine("DONE");
                Console.Write("Calculating ratings...");
                hensleyRatingVector = hensleyRating.GetRatings(matrix,
                                                RatingObject.Division);
                hensleySSVector = ComputeDivisionScheduleStrength(interDivisionGames, group, hensleyRatingVector);
                Console.WriteLine("DONE");
            }

            OutputResults(group, RatingObject.Division, interDivisionGames);
        }

        /// <summary>
        /// Writes the results to the specified CSV files and the results table in
        /// the database.
        /// </summary>
        /// <param name="group">The group being processed</param>
        /// <param name="ratingObject">
        /// Specifies the type of object the rating applies to
        /// </param>
        /// <param name="games">The games being processed</param>
        public void OutputResults(int group, RatingObject ratingObject,
                                    IEnumerable<Game> games)
        {
            StreamWriter sw;
            string fileName = String.Empty;
            string headerLine = "Team";
            string detailLine = String.Empty;
            int i = 1;

            Console.Write("\nWriting to output file...");

            string displayName = String.Empty;
            IOrderedEnumerable<int> sortedKeys = null;

            switch (ratingObject)
            {
                case RatingObject.Team:
                    fileName = String.Format("{0}_{1}.csv",
                                            settings.TeamResultsOutputFile, group);
                    break;
                case RatingObject.Conference:
                    fileName = String.Format("{0}_{1}.csv",
                                            settings.ConferenceResultsOutputFile, group);
                    break;
                case RatingObject.Division:
                    fileName = String.Format("{0}_{1}.csv",
                                            settings.DivisionResultsOutputFile, group);
                    break;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                sw = new StreamWriter(fileName);
                sw.WriteLine("Results for group {0}", group);

                if (settings.ComputeStandardRatings)
                {
                    headerLine = String.Format("{0},{1},ScheduleStrength", headerLine,
                                    StandardRating.RatingName);

                    sortedKeys = from k in stdRatingVector.Keys
                                 orderby stdRatingVector[k] descending
                                 select k;
                }

                if (settings.ComputeHomefieldAdvantageRatings)
                {
                    headerLine = String.Format("{0},{1},ScheduleStrength", headerLine,
                                    HomeFieldAdvantageRating.RatingName);

                    sortedKeys = from k in hfaRatingVector.Keys
                                 orderby hfaRatingVector[k] descending
                                 select k;
                }

                if (settings.ComputeMaxPointDifferentialRatings)
                {
                    headerLine = String.Format("{0},{1},ScheduleStrength", headerLine,
                                    MaxPointDifferentialRating.RatingName);

                    sortedKeys = from k in mpdRatingVector.Keys
                                 orderby mpdRatingVector[k] descending
                                 select k;
                }

                if (settings.ComputeHensleyRatings)
                {
                    headerLine = String.Format("{0},{1},ScheduleStrength", headerLine,
                                    HensleyRating.RatingName);

                    sortedKeys = from k in hensleyRatingVector.Keys
                                 orderby hensleyRatingVector[k] descending
                                 select k;
                }

                sw.WriteLine(headerLine);

                if (homeFieldAdvantage != 0)
                {
                    sw.WriteLine("Home Field Advantage,{0:G15}", homeFieldAdvantage);
                }

                foreach (int key in sortedKeys)
                {
                    switch (ratingObject)
                    {
                        case RatingObject.Team:
                            detailLine = WriteTeamResult(key);
                            break;

                        case RatingObject.Conference:
                            detailLine = WriteConferenceResult(key, games);
                            break;

                        case RatingObject.Division:
                            detailLine = WriteDivisionResult(key, games);
                            break;
                    }

                    sw.WriteLine(detailLine);
                    i++;
                }
                Console.WriteLine("DONE");
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n{0}", ex.Message);
            }

            // Write raw (Hensley-only, no ranks) CSV for teams
            if (ratingObject == RatingObject.Team && settings.ComputeHensleyRatings
                && hensleyRatingVector != null && sortedKeys != null)
            {
                string rawFileName = String.Format("{0}_Raw_{1}.csv",
                                                   settings.TeamResultsOutputFile, group);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(rawFileName));
                    using (StreamWriter rawSw = new StreamWriter(rawFileName))
                    {
                        foreach (int key in sortedKeys)
                        {
                            Team team = entities.Teams.First(t => t.ID == key);
                            double hensleySS = hensleySSVector != null
                                               && hensleySSVector.ContainsKey(key)
                                               ? hensleySSVector[key] : 0.0;
                            rawSw.WriteLine("{0},{1},{2},{3},{4}",
                                team.Name, team.Wins, team.Losses,
                                Math.Round(hensleyRatingVector[key], RoundDecimals),
                                Math.Round(hensleySS, RoundDecimals));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n{0}", ex.Message);
                }
            }

            try
            {
                Console.Write("Writing results to database...");
                entities.SaveChanges();
                Console.Write("DONE\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n{0}", ex.Message);
            }
        }

        /// <summary>
        /// Writes a record to the team result table in the database.
        /// </summary>
        /// <param name="key">The ID of the team</param>
        /// <returns>A comma delimited representation of the data</returns>
        private string WriteTeamResult(int key)
        {
            Team team = entities.Teams.First(t => t.ID == key);
            TeamResult teamResult;
            String detailLine = String.Format("{0} ({1}-{2})", team.Name, team.Wins,
                                            team.Losses);

            teamResult = entities.TeamResults.FirstOrDefault(tr => tr.TeamID == key
                                                                && tr.Year == settings.Year
                                                                && tr.Week == settings.Week);
            if (teamResult == null)
            {
                teamResult = new TeamResult { TeamID = key, Year = settings.Year, Week = settings.Week };
                entities.TeamResults.Add(teamResult);
            }

            // Use year-filtered game lists so W-L counts reflect only this season
            var homeGames = entities.Games.Where(g => g.HomeTeamID == key && g.Year == settings.Year).ToList();
            var awayGames = entities.Games.Where(g => g.AwayTeamID == key && g.Year == settings.Year).ToList();
            teamResult.Wins   = homeGames.Count(g => g.HomeScore > g.AwayScore)
                              + awayGames.Count(g => g.AwayScore > g.HomeScore);
            teamResult.Losses = homeGames.Count(g => g.HomeScore < g.AwayScore)
                              + awayGames.Count(g => g.AwayScore < g.HomeScore);

            if (settings.ComputeStandardRatings)
            {
                double ss = stdSSVector != null && stdSSVector.ContainsKey(key) ? stdSSVector[key] : 0.0;
                int ssRank = stdSSVector != null ? stdSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(stdRatingVector[key], RoundDecimals),
                                        stdRatingVector.Where(k => k.Value > stdRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                teamResult.StandardRating = stdRatingVector[key];
            }

            if (settings.ComputeHomefieldAdvantageRatings)
            {
                double ss = hfaSSVector != null && hfaSSVector.ContainsKey(key) ? hfaSSVector[key] : 0.0;
                int ssRank = hfaSSVector != null ? hfaSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(hfaRatingVector[key], RoundDecimals),
                                        hfaRatingVector.Where(k => k.Value > hfaRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                teamResult.HomefieldAdvantageRating = hfaRatingVector[key];
            }

            if (settings.ComputeMaxPointDifferentialRatings)
            {
                double ss = mpdSSVector != null && mpdSSVector.ContainsKey(key) ? mpdSSVector[key] : 0.0;
                int ssRank = mpdSSVector != null ? mpdSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(mpdRatingVector[key], RoundDecimals),
                                        mpdRatingVector.Where(k => k.Value > mpdRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                teamResult.MaxPointDifferentialRating = mpdRatingVector[key];
            }

            if (settings.ComputeHensleyRatings)
            {
                double ss = hensleySSVector != null && hensleySSVector.ContainsKey(key) ? hensleySSVector[key] : 0.0;
                int ssRank = hensleySSVector != null ? hensleySSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(hensleyRatingVector[key], RoundDecimals),
                                        hensleyRatingVector.Where(k => k.Value > hensleyRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                teamResult.HensleyRating = hensleyRatingVector[key];
            }

            if (hensleySSVector != null && hensleySSVector.ContainsKey(key))
                teamResult.ScheduleStrength = hensleySSVector[key];

            teamResult.PointsScored  = homeGames.Sum(g => g.HomeScore) + awayGames.Sum(g => g.AwayScore);
            teamResult.PointsAllowed = homeGames.Sum(g => g.AwayScore) + awayGames.Sum(g => g.HomeScore);

            return detailLine;
        }

        /// <summary>
        /// Writes a record to the conference result table in the database.
        /// </summary>
        /// <param name="key">The ID of the conference</param>
        /// <param name="interConferenceGames">The games being processes</param>
        /// <returns>A comma delimited representation of the data</returns>
        private string WriteConferenceResult(int key,
                                            IEnumerable<Game> interConferenceGames)
        {
            Conference conference = entities.Conferences.First(c => c.ID == key);
            ConferenceResult conferenceResult;
            int wins = entities.GetWins(interConferenceGames, conference);
            int losses = entities.GetLosses(interConferenceGames, conference);
            String detailLine = String.Format("{0} ({1}-{2})", conference.Name,
                                            wins, losses);

            conferenceResult = entities.ConferenceResults.FirstOrDefault(cr => cr.ConferenceID == key
                                                                              && cr.Year == settings.Year
                                                                              && cr.Week == settings.Week);
            if (conferenceResult == null)
            {
                conferenceResult = new ConferenceResult { ConferenceID = key, Year = settings.Year, Week = settings.Week };
                entities.ConferenceResults.Add(conferenceResult);
            }

            conferenceResult.Wins = wins;
            conferenceResult.Losses = losses;

            if (settings.ComputeStandardRatings)
            {
                double ss = stdSSVector != null && stdSSVector.ContainsKey(key) ? stdSSVector[key] : 0.0;
                int ssRank = stdSSVector != null ? stdSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(stdRatingVector[key], RoundDecimals),
                                        stdRatingVector.Where(k => k.Value > stdRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                conferenceResult.StandardRating = stdRatingVector[key];
            }

            if (settings.ComputeHomefieldAdvantageRatings)
            {
                double ss = hfaSSVector != null && hfaSSVector.ContainsKey(key) ? hfaSSVector[key] : 0.0;
                int ssRank = hfaSSVector != null ? hfaSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(hfaRatingVector[key], RoundDecimals),
                                        hfaRatingVector.Where(k => k.Value > hfaRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                conferenceResult.HomefieldAdvantageRating = hfaRatingVector[key];
            }

            if (settings.ComputeMaxPointDifferentialRatings)
            {
                double ss = mpdSSVector != null && mpdSSVector.ContainsKey(key) ? mpdSSVector[key] : 0.0;
                int ssRank = mpdSSVector != null ? mpdSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(mpdRatingVector[key], RoundDecimals),
                                        mpdRatingVector.Where(k => k.Value > mpdRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                conferenceResult.MaxPointDifferentialRating = mpdRatingVector[key];
            }

            if (settings.ComputeHensleyRatings)
            {
                double ss = hensleySSVector != null && hensleySSVector.ContainsKey(key) ? hensleySSVector[key] : 0.0;
                int ssRank = hensleySSVector != null ? hensleySSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(hensleyRatingVector[key], RoundDecimals),
                                        hensleyRatingVector.Where(k => k.Value > hensleyRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                conferenceResult.HensleyRating = hensleyRatingVector[key];
            }

            if (hensleySSVector != null && hensleySSVector.ContainsKey(key))
                conferenceResult.ScheduleStrength = hensleySSVector[key];

            return detailLine;
        }

        /// <summary>
        /// Writes a record to the division result table in the database.
        /// </summary>
        /// <param name="key">The ID of the division</param>
        /// <param name="interDivisionGames">The games being processes</param>
        /// <returns>A comma delimited representation of the data</returns>
        private string WriteDivisionResult(int key,
                                            IEnumerable<Game> interDivisionGames)
        {
            Division division = entities.Divisions.First(c => c.ID == key);
            DivisionResult divisionResult;
            int wins = entities.GetWins(interDivisionGames, division);
            int losses = entities.GetLosses(interDivisionGames, division);
            String detailLine = String.Format("{0} ({1}-{2})", division.Name, wins,
                                            losses);

            divisionResult = entities.DivisionResults.FirstOrDefault(dr => dr.DivisionID == key
                                                                          && dr.Year == settings.Year
                                                                          && dr.Week == settings.Week);
            if (divisionResult == null)
            {
                divisionResult = new DivisionResult { DivisionID = key, Year = settings.Year, Week = settings.Week };
                entities.DivisionResults.Add(divisionResult);
            }

            divisionResult.Wins = wins;
            divisionResult.Losses = losses;

            if (settings.ComputeStandardRatings)
            {
                double ss = stdSSVector != null && stdSSVector.ContainsKey(key) ? stdSSVector[key] : 0.0;
                int ssRank = stdSSVector != null ? stdSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(stdRatingVector[key], RoundDecimals),
                                        stdRatingVector.Where(k => k.Value > stdRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                divisionResult.StandardRating = stdRatingVector[key];
            }

            if (settings.ComputeHomefieldAdvantageRatings)
            {
                double ss = hfaSSVector != null && hfaSSVector.ContainsKey(key) ? hfaSSVector[key] : 0.0;
                int ssRank = hfaSSVector != null ? hfaSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(hfaRatingVector[key], RoundDecimals),
                                        hfaRatingVector.Where(k => k.Value > hfaRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                divisionResult.HomefieldAdvantageRating = hfaRatingVector[key];
            }

            if (settings.ComputeMaxPointDifferentialRatings)
            {
                double ss = mpdSSVector != null && mpdSSVector.ContainsKey(key) ? mpdSSVector[key] : 0.0;
                int ssRank = mpdSSVector != null ? mpdSSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(mpdRatingVector[key], RoundDecimals),
                                        mpdRatingVector.Where(k => k.Value > mpdRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                divisionResult.MaxPointDifferentialRating = mpdRatingVector[key];
            }

            if (settings.ComputeHensleyRatings)
            {
                double ss = hensleySSVector != null && hensleySSVector.ContainsKey(key) ? hensleySSVector[key] : 0.0;
                int ssRank = hensleySSVector != null ? hensleySSVector.Where(k => k.Value > ss).Count() + 1 : 0;
                detailLine = String.Format("{0},{1} ({2}),{3} ({4})", detailLine,
                                        Math.Round(hensleyRatingVector[key], RoundDecimals),
                                        hensleyRatingVector.Where(k => k.Value > hensleyRatingVector[key]).Count() + 1,
                                        Math.Round(ss, RoundDecimals),
                                        ssRank);
                divisionResult.HensleyRating = hensleyRatingVector[key];
            }

            if (hensleySSVector != null && hensleySSVector.ContainsKey(key))
                divisionResult.ScheduleStrength = hensleySSVector[key];

            return detailLine;
        }

        private Dictionary<int, double> ComputeTeamScheduleStrength(int group, Dictionary<int, double> ratingVector)
        {
            var result = new Dictionary<int, double>();
            foreach (var team in entities.Teams.Where(t => t.Group == group))
            {
                // Per-game average: count a repeat opponent once per game played
                var oppRatings = team.HomeGames.Select(g => g.AwayTeam)
                    .Concat(team.AwayGames.Select(g => g.HomeTeam))
                    .Where(o => ratingVector.ContainsKey(o.ID))
                    .Select(o => ratingVector[o.ID])
                    .ToList();
                result[team.ID] = oppRatings.Count > 0 ? oppRatings.Average() : 0.0;
            }
            return result;
        }

        private Dictionary<int, double> ComputeConferenceScheduleStrength(IEnumerable<Game> interConferenceGames, int group, Dictionary<int, double> ratingVector)
        {
            var result = new Dictionary<int, double>();
            var gamesList = interConferenceGames.ToList();
            foreach (var conference in entities.Conferences.Where(c => c.Group == group))
            {
                var oppConferenceIds = new List<int>();
                foreach (var game in gamesList)
                {
                    var homeTeam = entities.Teams.FirstOrDefault(t => t.ID == game.HomeTeamID);
                    var awayTeam = entities.Teams.FirstOrDefault(t => t.ID == game.AwayTeamID);
                    if (homeTeam != null && homeTeam.ConferenceID == conference.ID && awayTeam != null)
                        oppConferenceIds.Add(awayTeam.ConferenceID);
                    else if (awayTeam != null && awayTeam.ConferenceID == conference.ID && homeTeam != null)
                        oppConferenceIds.Add(homeTeam.ConferenceID);
                }
                var oppRatings = oppConferenceIds
                    .Where(id => ratingVector.ContainsKey(id))
                    .Select(id => ratingVector[id])
                    .ToList();
                result[conference.ID] = oppRatings.Count > 0 ? oppRatings.Average() : 0.0;
            }
            return result;
        }

        private Dictionary<int, double> ComputeDivisionScheduleStrength(IEnumerable<Game> interDivisionGames, int group, Dictionary<int, double> ratingVector)
        {
            var result = new Dictionary<int, double>();
            var gamesList = interDivisionGames.ToList();
            foreach (var division in entities.Divisions.Where(d => d.Group == group))
            {
                var oppDivisionIds = new List<int>();
                foreach (var game in gamesList)
                {
                    var homeTeam = entities.Teams.FirstOrDefault(t => t.ID == game.HomeTeamID);
                    var awayTeam = entities.Teams.FirstOrDefault(t => t.ID == game.AwayTeamID);
                    if (homeTeam != null && awayTeam != null)
                    {
                        var homeConf = entities.Conferences.FirstOrDefault(c => c.ID == homeTeam.ConferenceID);
                        var awayConf = entities.Conferences.FirstOrDefault(c => c.ID == awayTeam.ConferenceID);
                        if (homeConf != null && homeConf.DivisionID == division.ID && awayConf != null)
                            oppDivisionIds.Add(awayConf.DivisionID);
                        else if (awayConf != null && awayConf.DivisionID == division.ID && homeConf != null)
                            oppDivisionIds.Add(homeConf.DivisionID);
                    }
                }
                var oppRatings = oppDivisionIds
                    .Where(id => ratingVector.ContainsKey(id))
                    .Select(id => ratingVector[id])
                    .ToList();
                result[division.ID] = oppRatings.Count > 0 ? oppRatings.Average() : 0.0;
            }
            return result;
        }
    }
}
