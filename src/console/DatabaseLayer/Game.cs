/* Game.cs
 * Joel Hensley
 * January 18, 2010
 * This class is used to add addtional methods and properties
 * to the Game object.
 */
using System;

namespace DatabaseLayer
{
    public class Game
    {
        public int ID { get; set; }
        public int HomeTeamID { get; set; }
        public int AwayTeamID { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool IsNeutralSite { get; set; }
        public DateTime Date { get; set; }
        public int Year { get; set; }
        public virtual Team HomeTeam { get; set; }
        public virtual Team AwayTeam { get; set; }

        private int homeConferenceID = -1;
        /// <summary>
        /// Gets the conference ID of the home team
        /// </summary>
        public int HomeConferenceID
        {
            get
            {
                if (homeConferenceID != -1)
                {
                    return homeConferenceID;
                }
                else
                {
                    homeConferenceID = HomeTeam.ConferenceID;
                    return homeConferenceID;
                }
            }
        }

        private int awayConferenceID = -1;
        /// <summary>
        /// Gets the conference ID of the away team
        /// </summary>
        public int AwayConferenceID
        {
            get
            {
                if (awayConferenceID != -1)
                {
                    return awayConferenceID;
                }
                else
                {
                    awayConferenceID = AwayTeam.ConferenceID;
                    return awayConferenceID;
                }
            }
        }

        private int homeDivisionID = -1;
        /// <summary>
        /// Gets the division ID of the home team
        /// </summary>
        public int HomeDivisionID
        {
            get
            {
                if (homeDivisionID != -1)
                {
                    return homeDivisionID;
                }
                else
                {
                    homeDivisionID = HomeTeam.Conference.DivisionID;
                    return homeDivisionID;
                }
            }
        }

        private int awayDivisionID = -1;
        /// <summary>
        /// Gets the division ID of the away team
        /// </summary>
        public int AwayDivisionID
        {
            get
            {
                if (awayDivisionID != -1)
                {
                    return awayDivisionID;
                }
                else
                {
                    awayDivisionID = AwayTeam.Conference.DivisionID;
                    return awayDivisionID;
                }
            }
        }

        /// <summary>
        /// Gets the computed win-loss value for a game
        /// </summary>
        /// <param name="winnerScore">The winner score</param>
        /// <param name="loserScore">The loser score</param>
        /// <param name="lowerBounds">The lower bounds for the win-loss value</param>
        /// <param name="upperBounds">The upper bounds for the win-loss value</param>
        /// <returns>The computed win-loss value</returns>
        public static double GetHensleyPointDifferentialScore(double winnerScore,
                        double loserScore, double lowerBounds, double upperBounds)
        {
            double gameDifferential;
            double winnerLoserRatio;
            double computedDifferentialScore;

            gameDifferential = winnerScore - loserScore;
            winnerLoserRatio = loserScore / winnerScore;
            computedDifferentialScore = Math.Sqrt((1 - winnerLoserRatio)
                                                  * gameDifferential);

            if (computedDifferentialScore < lowerBounds)
            {
                // Adjust if below lower bounds
                computedDifferentialScore = lowerBounds;
            }
            else if (computedDifferentialScore > upperBounds)
            {
                // Adjust if above upper bounds
                computedDifferentialScore = upperBounds;
            }

            return computedDifferentialScore;
        }
    }
}
