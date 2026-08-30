/* Program.cs
 *
 * Joel Hensley
 * March 29, 2010
 *
 * This class is used to evaluate the different rating systems, write the results
 * to CSV files as well as the console.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DatabaseLayer;
using RatingSystem;

namespace RatingEvaluator
{
    class Program
    {
        private EvaluatorSetting settings;
        private CollegeFootballEntities entities;
        private Dictionary<int, int> stdRatingRankDictionary;
        private Dictionary<int, int> hfaRatingRankDictionary;
        private Dictionary<int, int> mpdRatingRankDictionary;
        private Dictionary<int, int> hensleyRatingRankDictionary;
        private int stdRatingMistakeCount;
        private int hfaRatingMistakeCount;
        private int mpdRatingMistakeCount;
        private int hensleyRatingMistakeCount;
        private double stdRatingError;
        private double hfaRatingError;
        private double mpdRatingError;
        private double hensleyRatingError;
        private int ratingCount;
        private double stdCorrelationCoefficientAPTop25;
        private double hfaCorrelationCoefficientAPTop25;
        private double mpdCorrelationCoefficientAPTop25;
        private double hensleyCorrelationCoefficientAPTop25;
        private double stdCorrelationCoefficientAvgFBSRanking;
        private double hfaCorrelationCoefficientAvgFBSRanking;
        private double mpdCorrelationCoefficientAvgFBSRanking;
        private double hensleyCorrelationCoefficientAvgFBSRanking;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Program()
        {
            settings = new EvaluatorSetting();
            entities = new CollegeFootballEntities();
            ratingCount = 0;
            stdCorrelationCoefficientAPTop25 = 0;
            hfaCorrelationCoefficientAPTop25 = 0;
            mpdCorrelationCoefficientAPTop25 = 0;
            hensleyCorrelationCoefficientAPTop25 = 0;

            stdRatingMistakeCount = 0;
            hfaRatingMistakeCount = 0;
            mpdRatingMistakeCount = 0;
            hensleyRatingMistakeCount = 0;

            stdRatingError = 0;
            hfaRatingError = 0;
            mpdRatingError = 0;
            hensleyRatingError = 0;

            stdRatingRankDictionary = new Dictionary<int, int>();
            hfaRatingRankDictionary = new Dictionary<int, int>();
            mpdRatingRankDictionary = new Dictionary<int, int>();
            hensleyRatingRankDictionary = new Dictionary<int, int>();
        }

        /// <summary>
        /// Calls run
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Program p = new Program();

            try
            {
                p.Run();
            }
            catch (Exception)
            {
                Console.WriteLine("An error occurred during execution.");
            }

            Console.WriteLine("\n\nPress any key to continue");
            Console.ReadKey();
        }

        /// <summary>
        /// Evaluates the four different rating systems and then writes the results
        /// to CSV files as well as the console.
        /// </summary>
        public void Run()
        {
            bool isHomeTeamWinner;
            bool isMistake;
            double error;
            int homeRank;
            int awayRank;

            Console.Write("Populating rank dictionaries...");
            PopulateRatingRankDictionaries();
            Console.WriteLine("DONE");

            Console.Write("Evaluating ranks...");
            foreach (Game game in entities.Games)
            {
                isHomeTeamWinner = (game.HomeScore > game.AwayScore);

                // Standard Rating
                homeRank = stdRatingRankDictionary[game.HomeTeamID];
                awayRank = stdRatingRankDictionary[game.AwayTeamID];
                error = GetError(homeRank, awayRank);
                isMistake = IsMistake(isHomeTeamWinner, homeRank, awayRank);

                if (isMistake)
                {
                    stdRatingMistakeCount++;
                    stdRatingError += error;
                }

                // Home Field Advantage Rating
                homeRank = hfaRatingRankDictionary[game.HomeTeamID];
                awayRank = hfaRatingRankDictionary[game.AwayTeamID];
                error = GetError(homeRank, awayRank);
                isMistake = IsMistake(isHomeTeamWinner, homeRank, awayRank);

                if (isMistake)
                {
                    hfaRatingMistakeCount++;
                    hfaRatingError += error;
                }

                // Max Point Differential Rating
                homeRank = mpdRatingRankDictionary[game.HomeTeamID];
                awayRank = mpdRatingRankDictionary[game.AwayTeamID];
                error = GetError(homeRank, awayRank);
                isMistake = IsMistake(isHomeTeamWinner, homeRank, awayRank);

                if (isMistake)
                {
                    mpdRatingMistakeCount++;
                    mpdRatingError += error;
                }

                // Hensley Rating
                homeRank = hensleyRatingRankDictionary[game.HomeTeamID];
                awayRank = hensleyRatingRankDictionary[game.AwayTeamID];
                error = GetError(homeRank, awayRank);
                isMistake = IsMistake(isHomeTeamWinner, homeRank, awayRank);

                if (isMistake)
                {
                    hensleyRatingMistakeCount++;
                    hensleyRatingError += error;
                }
            }

            ComputeAPTop25CorrelationCoefficient();
            ComputeAvgFBSRankingCorrelationCoefficient();
            Console.WriteLine("DONE");

            OutputResults();
        }

        /// <summary>
        /// Determines whether a lower ranked team beat a higher ranked team.
        /// </summary>
        /// <param name="isHomeTeamWinner">
        /// Whether or not the home team won the game
        /// </param>
        /// <param name="homeRank">The rank of the home team</param>
        /// <param name="awayRank">The rank of the away team</param>
        /// <returns>
        /// True if the lower ranked team won; otherwise, false.
        /// </returns>
        /// <remarks>
        /// The lower the rank, the better the team. The higher the rank,
        /// the worse the team.
        /// </remarks>
        private bool IsMistake(bool isHomeTeamWinner, int homeRank, int awayRank)
        {
            bool isMistake;

            // The lower the rank, the better the team
            // The higher the rank, the worse the team

            if (isHomeTeamWinner)
            {
                isMistake = homeRank > awayRank;
            }
            else
            {
                isMistake = awayRank > homeRank;
            }

            return isMistake;
        }

        /// <summary>
        /// Gets the size of the mistake using Potemkin's equation.
        /// </summary>
        /// <param name="homeRank">The rank of the home team</param>
        /// <param name="awayRank">The rank of the away team</param>
        /// <returns>The size of the mistake</returns>
        private double GetError(int homeRank, int awayRank)
        {
            double importance;
            double size;
            double error;

            size = ratingCount * Math.Abs(homeRank - awayRank) * 1.0
                         / (homeRank * awayRank);
            importance = ratingCount * (homeRank + awayRank)
                         / (homeRank * awayRank);
            error = size * importance;

            return error;
        }

        /// <summary>
        /// Computes the correlation coefficient against the final AP poll
        /// </summary>
        private void ComputeAPTop25CorrelationCoefficient()
        {
            ComputeCorrelationCoefficient(
                            GetRankingDictionary(settings.Top25Filename),
                            true);
        }

        /// <summary>
        /// Computes the correlation coefficient against the average FBS computer
        /// ranking
        /// </summary>
        private void ComputeAvgFBSRankingCorrelationCoefficient()
        {
            Division fbsDivision = entities.Divisions.OrderBy(d => d.ID).First();
            IEnumerable<Team> nonFbsTeams = from t in entities.Teams
                                             join c in entities.Conferences
                                                 on t.ConferenceID equals c.ID
                                             where c.DivisionID != fbsDivision.ID
                                             select t;

            // Remove all non-FBS teams from the rank dictionary
            foreach (Team nonFbsTeam in nonFbsTeams)
            {
                stdRatingRankDictionary.Remove(nonFbsTeam.ID);
                hfaRatingRankDictionary.Remove(nonFbsTeam.ID);
                mpdRatingRankDictionary.Remove(nonFbsTeam.ID);
                hensleyRatingRankDictionary.Remove(nonFbsTeam.ID);
            }

            ReassignPlaces(stdRatingRankDictionary);
            ReassignPlaces(hfaRatingRankDictionary);
            ReassignPlaces(mpdRatingRankDictionary);
            ReassignPlaces(hensleyRatingRankDictionary);

            ComputeCorrelationCoefficient(
                            GetRankingDictionary(settings.AvgFBSRankingFilename),
                            false);
        }

        /// <summary>
        /// Reassign the rank of all the teams since non-FBS schools were removed
        /// </summary>
        /// <param name="ratingRankDictionary">
        /// The dictionary being reassigned
        /// </param>
        private void ReassignPlaces(Dictionary<int, int> ratingRankDictionary)
        {
            int newPlace = 1;
            foreach (int key in ratingRankDictionary.OrderBy(kv =>
                            kv.Value).Select(kv => kv.Key).ToList())
            {
                ratingRankDictionary[key] = newPlace;
                newPlace++;
            }
        }

        /// <summary>
        /// Computes the correlation coefficient for a given ranking dictionary
        /// </summary>
        /// <param name="rankDictionary">
        /// The ranking dictionary being evaluated
        /// </param>
        /// <param name="isAPTop25">
        /// True if this for the final AP poll; otherwise, false.
        /// </param>
        private void ComputeCorrelationCoefficient(
                        Dictionary<int, int> rankDictionary, bool isAPTop25)
        {
            double sumX = 0;
            double sumX2 = 0;
            double sumStdRank = 0;
            double sumHfaRank = 0;
            double sumMpdRank = 0;
            double sumHensleyRank = 0;
            double sumStdRank2 = 0;
            double sumHfaRank2 = 0;
            double sumMpdRank2 = 0;
            double sumHensleyRank2 = 0;
            double sumStdRankX = 0;
            double sumHfaRankX = 0;
            double sumMpdRankX = 0;
            double sumHensleyRankX = 0;
            int elements = rankDictionary.Count;

            foreach (KeyValuePair<int, int> kvPair in rankDictionary)
            {
                sumX += kvPair.Value;
                sumX2 += kvPair.Value * kvPair.Value;

                sumStdRank += stdRatingRankDictionary[kvPair.Key];
                sumHfaRank += hfaRatingRankDictionary[kvPair.Key];
                sumMpdRank += mpdRatingRankDictionary[kvPair.Key];
                sumHensleyRank += hensleyRatingRankDictionary[kvPair.Key];

                sumStdRank2 += Math.Pow(stdRatingRankDictionary[kvPair.Key], 2);
                sumHfaRank2 += Math.Pow(hfaRatingRankDictionary[kvPair.Key], 2);
                sumMpdRank2 += Math.Pow(mpdRatingRankDictionary[kvPair.Key], 2);
                sumHensleyRank2 += Math.Pow(hensleyRatingRankDictionary[kvPair.Key],
                                             2);

                sumStdRankX += stdRatingRankDictionary[kvPair.Key] * kvPair.Value;
                sumHfaRankX += hfaRatingRankDictionary[kvPair.Key] * kvPair.Value;
                sumMpdRankX += mpdRatingRankDictionary[kvPair.Key] * kvPair.Value;
                sumHensleyRankX += hensleyRatingRankDictionary[kvPair.Key]
                                    * kvPair.Value;
            }

            if (isAPTop25)
            {
                stdCorrelationCoefficientAPTop25 =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumStdRank, sumStdRank2,
                                                   sumStdRankX);
                hfaCorrelationCoefficientAPTop25 =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumHfaRank, sumHfaRank2,
                                                   sumHfaRankX);
                mpdCorrelationCoefficientAPTop25 =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumMpdRank, sumMpdRank2,
                                                   sumMpdRankX);
                hensleyCorrelationCoefficientAPTop25 =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumHensleyRank, sumHensleyRank2,
                                                   sumHensleyRankX);
            }
            else
            {
                stdCorrelationCoefficientAvgFBSRanking =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumStdRank, sumStdRank2,
                                                   sumStdRankX);
                hfaCorrelationCoefficientAvgFBSRanking =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumHfaRank, sumHfaRank2,
                                                   sumHfaRankX);
                mpdCorrelationCoefficientAvgFBSRanking =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumMpdRank, sumMpdRank2,
                                                   sumMpdRankX);
                hensleyCorrelationCoefficientAvgFBSRanking =
                        GetCorrelationCoefficient(elements, sumX, sumX2,
                                                   sumHensleyRank, sumHensleyRank2,
                                                   sumHensleyRankX);
            }
        }

        /// <summary>
        /// Gets the correlation coefficient value
        /// </summary>
        /// <param name="elements">The number of elements</param>
        /// <param name="sumX">The sum of all the X values</param>
        /// <param name="sumX2">The sum of all the X^2 values</param>
        /// <param name="sumY">The sum of all the Y values</param>
        /// <param name="sumY2">The sum of all the Y^2 values</param>
        /// <param name="sumXY">The sum of all the X*Y values</param>
        /// <returns>The correlation coefficient</returns>
        private double GetCorrelationCoefficient(int elements, double sumX,
                        double sumX2, double sumY, double sumY2, double sumXY)
        {
            double r;

            r = (sumXY - sumX * sumY / elements)
                    / Math.Sqrt((sumX2 - Math.Pow(sumX, 2) / elements)
                                * (sumY2 - Math.Pow(sumY, 2) / elements));

            return r;
        }

        /// <summary>
        /// Creates a ranking dictionary for a given CSV file
        /// </summary>
        /// <param name="fileName">The path and name of the CSV file</param>
        /// <returns>The ranking dictionary</returns>
        /// <remarks>
        /// The CSV file should be in the following format: Place,TeamName
        /// </remarks>
        private Dictionary<int, int> GetRankingDictionary(string fileName)
        {
            Dictionary<int, int> rankDictionary = new Dictionary<int, int>();
            string line;
            string teamName;
            string[] values;
            int place = 0;
            Team team;

            try
            {
                StreamReader sr = new StreamReader(fileName);

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    values = line.Split(',');
                    teamName = values[1];

                    try
                    {
                        place = Convert.ToInt32(values[0]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Invalid place, {0}, in {1}", values[0],
                                        fileName);
                        continue;
                    }

                    team = entities.Teams.FirstOrDefault(t => t.Name == teamName);

                    if (team == null)
                    {
                        Console.WriteLine("Invalid team name, {0}, in {1}",
                                        teamName, fileName);
                        continue;
                    }

                    rankDictionary.Add(team.ID, place);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("An error occurred during the correlation " +
                                  "coefficient calculations.");
            }

            return rankDictionary;
        }

        /// <summary>
        /// Populates the four different rank dictionaries for the different methods
        /// </summary>
        private void PopulateRatingRankDictionaries()
        {
            IEnumerable<Team> teamsOrderByStdRating;
            IEnumerable<Team> teamsOrderByHfaRating;
            IEnumerable<Team> teamsOrderByMpdRating;
            IEnumerable<Team> teamsOrderByHensleyRating;

            teamsOrderByStdRating = entities.TeamResults.OrderByDescending(
                    tr => tr.StandardRating).Select(tr => tr.Team);
            teamsOrderByHfaRating = entities.TeamResults.OrderByDescending(
                    tr => tr.HomefieldAdvantageRating).Select(tr => tr.Team);
            teamsOrderByMpdRating = entities.TeamResults.OrderByDescending(
                    tr => tr.MaxPointDifferentialRating).Select(tr => tr.Team);
            teamsOrderByHensleyRating = entities.TeamResults.OrderByDescending(
                    tr => tr.HensleyRating).Select(tr => tr.Team);

            ratingCount = teamsOrderByStdRating.Count();
            for (int i = 1; i <= ratingCount; i++)
            {
                stdRatingRankDictionary.Add(teamsOrderByStdRating.ElementAt(
                                                i - 1).ID, i);
                hfaRatingRankDictionary.Add(teamsOrderByHfaRating.ElementAt(
                                                i - 1).ID, i);
                mpdRatingRankDictionary.Add(teamsOrderByMpdRating.ElementAt(
                                                i - 1).ID, i);
                hensleyRatingRankDictionary.Add(teamsOrderByHensleyRating.ElementAt(
                                                i - 1).ID, i);
            }
        }

        /// <summary>
        /// Outputs the results to the screen and the CSV files
        /// </summary>
        private void OutputResults()
        {
            int gameCount = entities.Games.Count();

            OutputToScreen(StandardRating.RatingName, stdRatingMistakeCount,
                            stdRatingError, stdCorrelationCoefficientAPTop25,
                            stdCorrelationCoefficientAvgFBSRanking, gameCount);
            OutputToScreen(HomeFieldAdvantageRating.RatingName,
                            hfaRatingMistakeCount, hfaRatingError,
                            hfaCorrelationCoefficientAPTop25,
                            hfaCorrelationCoefficientAvgFBSRanking, gameCount);
            OutputToScreen(MaxPointDifferentialRating.RatingName,
                            mpdRatingMistakeCount, mpdRatingError,
                            mpdCorrelationCoefficientAPTop25,
                            mpdCorrelationCoefficientAvgFBSRanking, gameCount);
            OutputToScreen(HensleyRating.RatingName, hensleyRatingMistakeCount,
                            hensleyRatingError, hensleyCorrelationCoefficientAPTop25,
                            hensleyCorrelationCoefficientAvgFBSRanking, gameCount);

            try
            {
                StreamWriter sw = new StreamWriter(settings.OutputFilename);
                sw.WriteLine("Rating Name,Number of Mistakes,Total Error,Correlation " +
                              "To Top 25,Correlation to Average FBS Computer " +
                              "Rankings");
                WriteResultLine(sw, StandardRating.RatingName,
                                stdRatingMistakeCount, stdRatingError,
                                stdCorrelationCoefficientAPTop25,
                                stdCorrelationCoefficientAvgFBSRanking, gameCount);
                WriteResultLine(sw, HomeFieldAdvantageRating.RatingName,
                                hfaRatingMistakeCount, hfaRatingError,
                                hfaCorrelationCoefficientAPTop25,
                                hfaCorrelationCoefficientAvgFBSRanking, gameCount);
                WriteResultLine(sw, MaxPointDifferentialRating.RatingName,
                                mpdRatingMistakeCount, mpdRatingError,
                                mpdCorrelationCoefficientAPTop25,
                                mpdCorrelationCoefficientAvgFBSRanking, gameCount);
                WriteResultLine(sw, HensleyRating.RatingName,
                                hensleyRatingMistakeCount, hensleyRatingError,
                                hensleyCorrelationCoefficientAPTop25,
                                hensleyCorrelationCoefficientAvgFBSRanking,
                                gameCount);
                sw.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("An error occurred during the writing of the " +
                                  "results.");
            }
        }

        /// <summary>
        /// Outputs the results to the screen
        /// </summary>
        /// <param name="ratingName">The name of the rating system</param>
        /// <param name="mistakeCount">The number of mistakes</param>
        /// <param name="totalError">The total error of the mistakes</param>
        /// <param name="correlationCoefficientAPTop25">
        /// The correlation coefficient to the final AP poll
        /// </param>
        /// <param name="correlationCoefficientAvgFBSRanking">
        /// The correlation coefficient to the average FBS ranking
        /// </param>
        /// <param name="gameCount">The number of games played</param>
        private void OutputToScreen(string ratingName, int mistakeCount,
                double totalError, double correlationCoefficientAPTop25,
                double correlationCoefficientAvgFBSRanking, int gameCount)
        {
            Console.WriteLine("Results for {0}", ratingName);
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Number of mistakes = {0} ({1}%)", mistakeCount,
                            Math.Round(mistakeCount * 100.0 / gameCount, 1));
            Console.WriteLine("Total error = {0}", Math.Round(totalError));
            Console.WriteLine("Correlation coefficient to Top 25 = {0}",
                            Math.Round(correlationCoefficientAPTop25, 2));
            Console.WriteLine("Correlation coefficient to Avg FBS Ranking = {0}",
                            Math.Round(correlationCoefficientAvgFBSRanking, 2));
            Console.WriteLine();
        }

        /// <summary>
        /// Outputs the results to the screen
        /// </summary>
        /// <param name="sw">The CSV file being written to</param>
        /// <param name="ratingName">The name of the rating system</param>
        /// <param name="mistakeCount">The number of mistakes</param>
        /// <param name="totalError">The total error of the mistakes</param>
        /// <param name="correlationCoefficientAPTop25">
        /// The correlation coefficient to the final AP poll
        /// </param>
        /// <param name="correlationCoefficientAvgFBSRanking">
        /// The correlation coefficient to the average FBS ranking
        /// </param>
        /// <param name="gameCount">The number of games played</param>
        private void WriteResultLine(StreamWriter sw, string ratingName,
                int mistakeCount, double totalError,
                double correlationCoefficientAPTop25,
                double correlationCoefficientAvgFBSRanking, int gameCount)
        {
            sw.WriteLine("{0},{1} ({2}%),{3},{4},{5}", ratingName, mistakeCount,
                        Math.Round(mistakeCount * 100.0 / gameCount, 1),
                        Math.Round(totalError),
                        Math.Round(correlationCoefficientAPTop25, 2),
                        Math.Round(correlationCoefficientAvgFBSRanking, 2));
        }
    }
}
