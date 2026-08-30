/* MaxPointDifferentialRating.cs
 * Joel Hensley
 * January 18, 2010
 * This class extends the standard rating by limiting the point
 * differential for any given game.
 */
using System.Collections.Generic;
using System.Linq;
using DatabaseLayer;

namespace RatingSystem
{
    public class MaxPointDifferentialRating : StandardRating
    {
        public static new string RatingName = "Max Point Differential Ratings";
        private int maxPointDifferential = 28;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        /// <param name="_group">The group to calculate ratings for</param>
        public MaxPointDifferentialRating(CollegeFootballEntities _entities,
                                          int _group)
            : base(_entities, _group)
        {
        }

        /// <summary>
        /// Overloaded constructor that sets the max point differential
        /// </summary>
        /// <param name="_entities">The database object</param>
        /// <param name="_group">The group to calculate ratings for</param>
        /// <param name="_maxPointDifferential">The max point differential</param>
        public MaxPointDifferentialRating(CollegeFootballEntities _entities,
                                          int _group, int _maxPointDifferential)
            : base(_entities, _group)
        {
            maxPointDifferential = _maxPointDifferential;
        }

        /// <summary>
        /// Creates the team matrix to solve. Limits the point differential
        /// to the maxPointDifferential value.
        /// </summary>
        /// <returns>The team matrix</returns>
        public override double[][] CreateTeamMatrix()
        {
            int rowCount = 0;
            int gameCount = 0;
            double[][] matrix = new double[teamCount][];

            foreach (Team team in entities.Teams.Where(t => t.Group ==
                            group).OrderBy(t => t.ID))
            {
                gameCount = team.Games.Count();
                matrix[rowCount] = new double[teamCount + 1];

                // Diaganol value is the number of games played
                matrix[teamIDMapping[team.ID]][teamIDMapping[team.ID]] = gameCount;

                // Right hand side of every line is total score margin
                matrix[rowCount][teamCount] =
                        team.GetPointDifferential(maxPointDifferential);
                rowCount++;
            }

            foreach (Game game in entities.GetGames(group))
            {
                matrix[teamIDMapping[game.HomeTeamID]][teamIDMapping[game.AwayTeamID]] -= 1;
                matrix[teamIDMapping[game.AwayTeamID]][teamIDMapping[game.HomeTeamID]] -= 1;
            }

            // Last row should be all ones and then a zero for RHS
            for (int col = 0; col < teamCount; col++)
            {
                matrix[teamCount - 1][col] = 1;
            }
            matrix[teamCount - 1][teamCount] = 0;

            return matrix;
        }

        /// <summary>
        /// Creates the conference matrix to solve. Limits the point differential
        /// to the maxPointDifferential value.
        /// </summary>
        /// <param name="interConferenceGames">
        /// The games between conferences in the group
        /// </param>
        /// <returns>The conference matrix</returns>
        public override double[][] CreateConferenceMatrix(
                        IEnumerable<Game> interConferenceGames)
        {
            int rowCount = 0;
            int gameCount = 0;
            double[][] matrix = new double[conferenceCount][];
            IEnumerable<Game> conferenceGames;

            foreach (Conference conference in entities.Conferences.Where(c =>
                            c.Group == group).OrderBy(c => c.ID))
            {
                conferenceGames = entities.GetGames(conference);
                gameCount = conferenceGames.Count();

                matrix[rowCount] = new double[conferenceCount + 1];

                // Diaganol value is the number of games played
                matrix[conferenceIDMapping[conference.ID]]
                      [conferenceIDMapping[conference.ID]] = gameCount;

                // Right hand side of every line is total score margin
                matrix[rowCount][conferenceCount] =
                        entities.GetPointDifferential(conferenceGames, conference,
                                                      maxPointDifferential);
                rowCount++;
            }

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

            return matrix;
        }

        /// <summary>
        /// Creates the division matrix to solve. Limits the point differential
        /// to the maxPointDifferential value.
        /// </summary>
        /// <param name="interDivisionGames">
        /// The games between divisions in the group
        /// </param>
        /// <returns>The division matrix</returns>
        public override double[][] CreateDivisionMatrix(
                        IEnumerable<Game> interDivisionGames)
        {
            int rowCount = 0;
            int gameCount = 0;
            double[][] matrix = new double[divisionCount][];
            IEnumerable<Game> divisionGames;

            foreach (Division division in entities.Divisions.Where(d => d.Group ==
                            group).OrderBy(d => d.ID))
            {
                divisionGames = entities.GetGames(division);
                gameCount = divisionGames.Count();

                matrix[rowCount] = new double[divisionCount + 1];

                // Diaganol value is the number of games played
                matrix[divisionIDMapping[division.ID]]
                      [divisionIDMapping[division.ID]] = gameCount;

                // Right hand side of every line is total score margin
                matrix[rowCount][divisionCount] =
                        entities.GetPointDifferential(divisionGames, division,
                                                      maxPointDifferential);
                rowCount++;
            }

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

            return matrix;
        }
    }
}
