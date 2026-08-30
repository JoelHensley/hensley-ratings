/* HensleyRating.cs
 * Joel Hensley
 * January 18, 2010
 * This class derives from the home field advantage rating class.
 * Instead of using the points differential, this class comptues
 * the win or loss value using a function based on the winning
 * score and losing score. A user can define the upper and lower
 * bounds of this value if desired; otherwise, it defaults to
 * 4.0 and 1.0, respectively.
 */
using System.Collections.Generic;
using System.Linq;
using DatabaseLayer;

namespace RatingSystem
{
    public class HensleyRating : HomeFieldAdvantageRating
    {
        public static new string RatingName = "Hensley Ratings";

        private double upperBounds = 4.0F;
        /// <summary>
        /// The upper bounds to the win-loss value function.
        /// </summary>
        public double UpperBounds
        {
            get
            {
                return upperBounds;
            }
        }

        private double lowerBounds = 1.0F;
        /// <summary>
        /// The lower bounds to the win-loss value function.
        /// </summary>
        public double LowerBounds
        {
            get
            {
                return lowerBounds;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        /// <param name="_group">The group being processed</param>
        public HensleyRating(CollegeFootballEntities _entities, int _group)
            : base(_entities, _group)
        {
        }

        /// <summary>
        /// User-defined constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        /// <param name="_group">The group being processed</param>
        /// <param name="_lowerBounds">
        /// The lower bounds to the win-loss value function
        /// </param>
        /// <param name="_upperBounds">
        /// The upper bounds to the win-loss value function
        /// </param>
        public HensleyRating(CollegeFootballEntities _entities, int _group,
                              double _lowerBounds, double _upperBounds)
            : base(_entities, _group)
        {
            upperBounds = _upperBounds;
            lowerBounds = _lowerBounds;
        }

        /// <summary>
        /// Populates the rating matrix to be solved for teams.
        /// </summary>
        /// <returns>The populated rating matrix.</returns>
        public override double[][] CreateTeamMatrix()
        {
            int rowCount = 0;
            int gameCount = 0;
            double[][] matrix = new double[teamCount + 1][];
            IEnumerable<Game> groupGames = entities.GetGames(group);

            matrix[teamCount] = new double[teamCount + 2];
            foreach (Team team in entities.Teams.Where(t => t.Group ==
                            group).OrderBy(t => t.ID))
            {
                gameCount = team.Games.Count();
                matrix[rowCount] = new double[teamCount + 2];

                // Second to last column is home game differential
                matrix[rowCount][teamCount] = team.HomeGameDifferential;
                matrix[teamCount][rowCount] = team.HomeGameDifferential;

                // Diaganol value is the number of games played
                matrix[rowCount][teamIDMapping[team.ID]] = gameCount;

                // Right hand side of every line is the calculated score margin
                matrix[rowCount][teamCount + 1] =
                        team.GetHensleyPointDifferential(lowerBounds, upperBounds);
                rowCount++;
            }
            matrix[teamCount][teamCount] = entities.GetHomeGameCount(groupGames);
            matrix[teamCount][teamCount + 1] =
                    entities.GetHomeGameHensleyPointDifferential(groupGames,
                                                                  lowerBounds,
                                                                  upperBounds);

            foreach (Game game in groupGames)
            {
                matrix[teamIDMapping[game.HomeTeamID]][teamIDMapping[game.AwayTeamID]] -= 1;
                matrix[teamIDMapping[game.AwayTeamID]][teamIDMapping[game.HomeTeamID]] -= 1;
            }

            // Last row should be all ones and then a zero for homefield advantage
            // and a 0 for the RHS
            for (int col = 0; col < teamCount; col++)
            {
                matrix[teamCount - 1][col] = 1;
            }
            matrix[teamCount - 1][teamCount] = 0;
            matrix[teamCount - 1][teamCount + 1] = 0;

            return matrix;
        }

        /// <summary>
        /// Populates the rating matrix to be solved for conferences.
        /// </summary>
        /// <param name="interConferenceGames">
        /// All interconference games played.
        /// </param>
        /// <returns>The populated rating matrix.</returns>
        public override double[][] CreateConferenceMatrix(
                        IEnumerable<Game> interConferenceGames)
        {
            int rowCount = 0;
            int gameCount = 0;
            int homeGameDifferential;
            double[][] matrix = new double[conferenceCount + 1][];
            IEnumerable<Game> conferenceGames;

            matrix[conferenceCount] = new double[conferenceCount + 2];
            foreach (Conference conference in entities.Conferences.Where(c =>
                            c.Group == group).OrderBy(c => c.ID))
            {
                conferenceGames = entities.GetGames(conference);
                gameCount = conferenceGames.Count();

                matrix[rowCount] = new double[conferenceCount + 2];

                // Diaganol value is the number of games played
                matrix[conferenceIDMapping[conference.ID]]
                      [conferenceIDMapping[conference.ID]] = gameCount;

                // Second to last column is home game differential
                homeGameDifferential =
                        entities.GetHomeGameDifferential(conferenceGames, conference);
                matrix[rowCount][conferenceCount] = homeGameDifferential;
                matrix[conferenceCount][rowCount] = homeGameDifferential;

                // Right hand side of every line is total score margin
                matrix[rowCount][conferenceCount + 1] =
                        entities.GetHensleyPointDifferential(conferenceGames,
                                                             conference, lowerBounds,
                                                             upperBounds);
                rowCount++;
            }
            matrix[conferenceCount][conferenceCount] =
                    entities.GetHomeGameCount(interConferenceGames);
            matrix[conferenceCount][conferenceCount + 1] =
                    entities.GetHomeGameHensleyPointDifferential(interConferenceGames,
                                                                  lowerBounds,
                                                                  upperBounds);

            foreach (Game game in interConferenceGames)
            {
                matrix[conferenceIDMapping[game.HomeConferenceID]]
                      [conferenceIDMapping[game.AwayConferenceID]] -= 1;
                matrix[conferenceIDMapping[game.AwayConferenceID]]
                      [conferenceIDMapping[game.HomeConferenceID]] -= 1;
            }

            // Last row should be all ones and then a zero for RHS
            for (int col = 0; col < conferenceCount; col++)
            {
                matrix[conferenceCount - 1][col] = 1;
            }
            matrix[conferenceCount - 1][conferenceCount] = 0;
            matrix[conferenceCount - 1][conferenceCount + 1] = 0;

            return matrix;
        }

        /// <summary>
        /// Populates the rating matrix to be solved for divisions.
        /// </summary>
        /// <param name="interDivisionGames">
        /// All interdivision games played.
        /// </param>
        /// <returns>The populated rating matrix.</returns>
        public override double[][] CreateDivisionMatrix(
                        IEnumerable<Game> interDivisionGames)
        {
            int rowCount = 0;
            int gameCount = 0;
            int homeGameDifferential;
            double[][] matrix = new double[divisionCount + 1][];
            IEnumerable<Game> divisionGames;

            matrix[divisionCount] = new double[divisionCount + 2];
            foreach (Division division in entities.Divisions.Where(d => d.Group ==
                            group).OrderBy(d => d.ID))
            {
                divisionGames = entities.GetGames(division);
                gameCount = divisionGames.Count();

                matrix[rowCount] = new double[divisionCount + 2];

                // Diaganol value is the number of games played
                matrix[divisionIDMapping[division.ID]]
                      [divisionIDMapping[division.ID]] = gameCount;

                // Second to last column is home game differential
                homeGameDifferential =
                        entities.GetHomeGameDifferential(divisionGames, division);
                matrix[rowCount][divisionCount] = homeGameDifferential;
                matrix[divisionCount][rowCount] = homeGameDifferential;

                // Right hand side of every line is total score margin
                matrix[rowCount][divisionCount + 1] =
                        entities.GetHensleyPointDifferential(divisionGames, division,
                                                             lowerBounds, upperBounds);
                rowCount++;
            }
            matrix[divisionCount][divisionCount] =
                    entities.GetHomeGameCount(interDivisionGames);
            matrix[divisionCount][divisionCount + 1] =
                    entities.GetHomeGameHensleyPointDifferential(interDivisionGames,
                                                                  lowerBounds,
                                                                  upperBounds);

            foreach (Game game in interDivisionGames)
            {
                matrix[divisionIDMapping[game.HomeDivisionID]]
                      [divisionIDMapping[game.AwayDivisionID]] -= 1;
                matrix[divisionIDMapping[game.AwayDivisionID]]
                      [divisionIDMapping[game.HomeDivisionID]] -= 1;
            }

            // Last row should be all ones and then a zero for RHS
            for (int col = 0; col < divisionCount; col++)
            {
                matrix[divisionCount - 1][col] = 1;
            }
            matrix[divisionCount - 1][divisionCount] = 0;
            matrix[divisionCount - 1][divisionCount + 1] = 0;

            return matrix;
        }
    }
}
