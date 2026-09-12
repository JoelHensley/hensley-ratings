/* Team.cs
 * Joel Hensley
 * January 16, 2010
 * This class is used to add addtional methods and properties
 * to the Team object.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseLayer
{
    public class Team
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ConferenceID { get; set; }
        public int? Group { get; set; }
        public string TwitterHandle { get; set; }
        public string TwitterWidgetId { get; set; }
        public virtual Conference Conference { get; set; }
        public virtual ICollection<Game> HomeGames { get; set; }
        public virtual ICollection<Game> AwayGames { get; set; }
        public virtual ICollection<TeamAffiliation> TeamAffiliations { get; set; }

        private bool isHomeGameDifferentialSet = false;
        private int homeGameDifferential = 0;
        /// <summary>
        /// Gets the total number of home games minues
        /// the total number of away games
        /// </summary>
        public int HomeGameDifferential
        {
            get
            {
                if (isHomeGameDifferentialSet)
                {
                    return homeGameDifferential;
                }
                else
                {
                    isHomeGameDifferentialSet = true;

                    homeGameDifferential = HomeGames.Where(g => g.IsNeutralSite
                                                == false).Count()
                        - AwayGames.Where(g => g.IsNeutralSite == false).Count();

                    return homeGameDifferential;
                }
            }
        }

        private int pointDifferential = 0;
        private bool isPointDifferentialSet = false;
        /// <summary>
        /// Gets the total point differential
        /// </summary>
        public int PointDifferential
        {
            get
            {
                if (isPointDifferentialSet)
                {
                    return pointDifferential;
                }
                else
                {
                    isPointDifferentialSet = true;

                    foreach (Game game in HomeGames)
                    {
                        pointDifferential += (game.HomeScore - game.AwayScore);
                    }

                    foreach (Game game in AwayGames)
                    {
                        pointDifferential += (game.AwayScore - game.HomeScore);
                    }

                    return pointDifferential;
                }
            }
        }

        /// <summary>
        /// Gets the total point differential using a specified
        /// max point differential
        /// </summary>
        /// <param name="maxPointDifferential">The maximum point differential for each game</param>
        /// <returns>The total point differential</returns>
        public int GetPointDifferential(int maxPointDifferential)
        {
            int gameDifferential;
            int calculatedPointDifferential = 0;

            foreach (Game game in HomeGames)
            {
                gameDifferential = game.HomeScore - game.AwayScore;
                gameDifferential = (Math.Abs(gameDifferential) > maxPointDifferential)
                            ? maxPointDifferential * Math.Sign(gameDifferential)
                            : gameDifferential;
                calculatedPointDifferential += gameDifferential;
            }

            foreach (Game game in AwayGames)
            {
                gameDifferential = game.AwayScore - game.HomeScore;
                gameDifferential = (Math.Abs(gameDifferential) > maxPointDifferential)
                            ? maxPointDifferential * Math.Sign(gameDifferential)
                            : gameDifferential;
                calculatedPointDifferential += gameDifferential;
            }

            return calculatedPointDifferential;
        }

        /// <summary>
        /// Gets the total computed win-loss value using a specified
        /// upper and lower bounds.
        /// </summary>
        /// <param name="lowerBounds">The lower bounds to the win-loss value</param>
        /// <param name="upperBounds">The upper bounds to the win-loss value</param>
        /// <returns>The total computed win-loss value</returns>
        public double GetHensleyPointDifferential(double lowerBounds,
                double upperBounds)
        {
            bool isWinner;
            double winnerScore;
            double loserScore;
            double computedDifferentialScore;
            double cumulativeTotal = 0;

            foreach (Game game in HomeGames)
            {
                isWinner = (game.HomeScore > game.AwayScore);
                if (isWinner)
                {
                    winnerScore = (double)game.HomeScore;
                    loserScore = (double)game.AwayScore;
                }
                else
                {
                    winnerScore = (double)game.AwayScore;
                    loserScore = (double)game.HomeScore;
                }

                computedDifferentialScore = Game.GetHensleyPointDifferentialScore(
                                winnerScore, loserScore, lowerBounds, upperBounds);

                if (!isWinner)
                {
                    // The loser receives the negative value of this computation
                    computedDifferentialScore *= -1;
                }

                cumulativeTotal += computedDifferentialScore;
            }

            foreach (Game game in AwayGames)
            {
                isWinner = (game.AwayScore > game.HomeScore);
                if (isWinner)
                {
                    winnerScore = (double)game.AwayScore;
                    loserScore = (double)game.HomeScore;
                }
                else
                {
                    winnerScore = (double)game.HomeScore;
                    loserScore = (double)game.AwayScore;
                }

                computedDifferentialScore = Game.GetHensleyPointDifferentialScore(
                                winnerScore, loserScore, lowerBounds, upperBounds);

                if (!isWinner)
                {
                    // The loser receives the negative value of this computation
                    computedDifferentialScore *= -1;
                }

                cumulativeTotal += computedDifferentialScore;
            }

            return cumulativeTotal;
        }

        private IEnumerable<Game> games = null;
        /// <summary>
        /// Gets the union of all away games and home games
        /// </summary>
        public IEnumerable<Game> Games
        {
            get
            {
                if (games != null)
                {
                    return games;
                }
                else
                {
                    games = HomeGames.Union(AwayGames).OrderBy(g => g.Date).ToList();
                    return games;
                }
            }
        }

        private int wins = -1;
        /// <summary>
        /// Gets the number of games won
        /// </summary>
        public int Wins
        {
            get
            {
                if (wins != -1)
                {
                    return wins;
                }
                else
                {
                    wins = HomeGames.Where(g => g.HomeScore > g.AwayScore).Count()
                         + AwayGames.Where(g => g.AwayScore > g.HomeScore).Count();
                    return wins;
                }
            }
        }

        private int losses = -1;
        /// <summary>
        /// Gets the number of games lost
        /// </summary>
        public int Losses
        {
            get
            {
                if (losses != -1)
                {
                    return losses;
                }
                else
                {
                    losses = HomeGames.Where(g => g.HomeScore < g.AwayScore).Count()
                           + AwayGames.Where(g => g.AwayScore < g.HomeScore).Count();
                    return losses;
                }
            }
        }

        private IEnumerable<Team> opponents = null;
        /// <summary>
        /// Gets all opponents
        /// </summary>
        public IEnumerable<Team> Opponents
        {
            get
            {
                if (opponents != null)
                {
                    return opponents;
                }
                else
                {
                    opponents = HomeGames.Select(g => g.AwayTeam)
                                        .Union(AwayGames.Select(g => g.HomeTeam))
                                        .ToList();

                    return opponents;
                }
            }
        }
    }
}
