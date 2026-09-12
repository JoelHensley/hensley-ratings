/* HomeFieldAdvantageRating.cs
 * Joel Hensley
 * January 18, 2010
 * This class extends the standard rating by adding a home field
 * advantage variable to the ratings system.
 */
using System.Collections.Generic;
using System.Linq;
using DatabaseLayer;

namespace RatingSystem
{
    public class HomeFieldAdvantageRating : StandardRating
    {
        public static new string RatingName = "Home Field Advantage Ratings";
        protected double homeFieldAdvantage = 0;

        /// <summary>
        /// Returns the home field advantage value
        /// </summary>
        public double HomeFieldAdvantage
        {
            get { return homeFieldAdvantage; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        /// <param name="_group">The group to calculate ratings for</param>
        public HomeFieldAdvantageRating(CollegeFootballEntities _entities,
                                        int _group)
            : base(_entities, _group)
        {
        }

        /// <summary>
        /// Calls GaussJordanElimination to solve the matrix and then maps the
        /// results back to their team, conference, or division IDs. The last
        /// value in the solution vector is the home field advantage.
        /// </summary>
        /// <param name="matrix">The matrix to solve</param>
        /// <param name="ratingObject">
        /// Indicates the type of object the rating applies to
        /// </param>
        /// <returns>A dictionary of IDs to ratings</returns>
        public override Dictionary<int, double> GetRatings(double[][] matrix,
                                                            RatingObject ratingObject)
        {
            Dictionary<int, double> ratingDictionary = new Dictionary<int, double>();
            GaussJordanElimination gj = new GaussJordanElimination();
            double[] ratingVector;
            int rowCount;

            rowCount = matrix.Count();
            ratingVector = gj.PerformElimination(matrix, rowCount, rowCount + 1);

            // If the solver returns null the HFA column is linearly dependent
            // (e.g. every team has a perfectly balanced home/away schedule).
            // Fall back to base-class standard-rating behaviour with HFA = 0.
            if (ratingVector == null)
            {
                homeFieldAdvantage = 0;
                if (ratingObject == RatingObject.Team)
                    return base.GetRatings(base.CreateTeamMatrix(), ratingObject);
                // Conference / Division singular cases: return zeros
                if (ratingObject == RatingObject.Conference)
                    foreach (var kv in conferenceIDMapping)
                        ratingDictionary[kv.Key] = 0.0;
                else
                    foreach (var kv in divisionIDMapping)
                        ratingDictionary[kv.Key] = 0.0;
                return ratingDictionary;
            }

            // The last row is the home field advantage value
            homeFieldAdvantage = ratingVector[rowCount - 1];

            for (int row = 0; row < rowCount - 1; row++)
            {
                switch (ratingObject)
                {
                    case RatingObject.Team:
                        ratingDictionary.Add(teamIDMapping.First(t => t.Value ==
                                            row).Key, ratingVector[row]);
                        break;
                    case RatingObject.Conference:
                        ratingDictionary.Add(conferenceIDMapping.First(c => c.Value
                                            == row).Key, ratingVector[row]);
                        break;
                    case RatingObject.Division:
                        ratingDictionary.Add(divisionIDMapping.First(d => d.Value ==
                                            row).Key, ratingVector[row]);
                        break;
                }
            }

            return ratingDictionary;
        }

        /// <summary>
        /// Creates the team matrix to solve. Adds a column for home field
        /// advantage.
        /// </summary>
        /// <returns>The team matrix</returns>
        public override double[][] CreateTeamMatrix()
        {
            int rowCount = 0;
            int gameCount = 0;
            double[][] matrix = new double[teamCount + 1][];
            IEnumerable<Game> groupGames;

            groupGames = entities.GetGames(group, ratingSettings.Year);

            // Pre-allocate the HFA equation row (row teamCount)
            matrix[teamCount] = new double[teamCount + 2];

            foreach (Team team in entities.Teams.Where(t => t.Group ==
                            group).OrderBy(t => t.ID))
            {
                gameCount = team.Games.Count();
                matrix[rowCount] = new double[teamCount + 2];

                // Diaganol value is the number of games played
                matrix[teamIDMapping[team.ID]][teamIDMapping[team.ID]] = gameCount;

                // Second to last column is the home game differential
                matrix[rowCount][teamCount] = team.HomeGameDifferential;

                // Symmetric: this team's rating coefficient in the HFA equation
                matrix[teamCount][rowCount] = team.HomeGameDifferential;

                // Right hand side of every line is total score margin
                matrix[rowCount][teamCount + 1] = team.PointDifferential;
                rowCount++;
            }

            // HFA equation: diagonal = total non-neutral home games, RHS = home point diff
            matrix[teamCount][teamCount] = entities.GetHomeGameCount(groupGames);
            matrix[teamCount][teamCount + 1] = entities.GetHomeGamePointDifferential(groupGames);

            foreach (Game game in groupGames)
            {
                matrix[teamIDMapping[game.HomeTeamID]][teamIDMapping[game.AwayTeamID]] -= 1;
                matrix[teamIDMapping[game.AwayTeamID]][teamIDMapping[game.HomeTeamID]] -= 1;
            }

            // Anchor overwrites the last team's row (same pattern as StandardRating)
            for (int col = 0; col < teamCount; col++)
            {
                matrix[teamCount - 1][col] = 1;
            }
            matrix[teamCount - 1][teamCount] = 0;
            matrix[teamCount - 1][teamCount + 1] = 0;

            return matrix;
        }

        /// <summary>
        /// Creates the conference matrix to solve. Adds a column for home field
        /// advantage.
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
            int homeGameDifferential;
            double[][] matrix = new double[conferenceCount + 1][];
            IEnumerable<Game> conferenceGames;

            // Pre-allocate the HFA equation row
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

                // Second to last column is the home game differential
                homeGameDifferential =
                        entities.GetHomeGameDifferential(conferenceGames, conference);
                matrix[rowCount][conferenceCount] = homeGameDifferential;

                // Symmetric: this conference's rating coefficient in the HFA equation
                matrix[conferenceCount][rowCount] = homeGameDifferential;

                // Right hand side of every line is total score margin
                matrix[rowCount][conferenceCount + 1] =
                        entities.GetPointDifferential(conferenceGames, conference);
                rowCount++;
            }

            // HFA equation: diagonal = total non-neutral home games, RHS = home point diff
            matrix[conferenceCount][conferenceCount] =
                    entities.GetHomeGameCount(interConferenceGames);
            matrix[conferenceCount][conferenceCount + 1] =
                    entities.GetHomeGamePointDifferential(interConferenceGames);

            foreach (Game game in interConferenceGames)
            {
                matrix[conferenceIDMapping[game.HomeConferenceID]]
                      [conferenceIDMapping[game.AwayConferenceID]] -= 1;
                matrix[conferenceIDMapping[game.AwayConferenceID]]
                      [conferenceIDMapping[game.HomeConferenceID]] -= 1;
            }

            // Anchor overwrites the last conference's row
            for (int col = 0; col < conferenceCount; col++)
            {
                matrix[conferenceCount - 1][col] = 1;
            }
            matrix[conferenceCount - 1][conferenceCount] = 0;
            matrix[conferenceCount - 1][conferenceCount + 1] = 0;

            return matrix;
        }

        /// <summary>
        /// Creates the division matrix to solve. Adds a column for home field
        /// advantage.
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
            int homeGameDifferential;
            double[][] matrix = new double[divisionCount + 1][];
            IEnumerable<Game> divisionGames;

            // Pre-allocate the HFA equation row
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

                // Second to last column is the home game differential
                homeGameDifferential =
                        entities.GetHomeGameDifferential(divisionGames, division);
                matrix[rowCount][divisionCount] = homeGameDifferential;

                // Symmetric: this division's rating coefficient in the HFA equation
                matrix[divisionCount][rowCount] = homeGameDifferential;

                // Right hand side of every line is total score margin
                matrix[rowCount][divisionCount + 1] =
                        entities.GetPointDifferential(divisionGames, division);
                rowCount++;
            }

            // HFA equation: diagonal = total non-neutral home games, RHS = home point diff
            matrix[divisionCount][divisionCount] =
                    entities.GetHomeGameCount(interDivisionGames);
            matrix[divisionCount][divisionCount + 1] =
                    entities.GetHomeGamePointDifferential(interDivisionGames);

            foreach (Game game in interDivisionGames)
            {
                matrix[divisionIDMapping[game.HomeDivisionID]]
                      [divisionIDMapping[game.AwayDivisionID]] -= 1;
                matrix[divisionIDMapping[game.AwayDivisionID]]
                      [divisionIDMapping[game.HomeDivisionID]] -= 1;
            }

            // Anchor overwrites the last division's row
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
