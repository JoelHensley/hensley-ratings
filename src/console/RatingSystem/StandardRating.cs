/* StandardRating.cs
 * Joel Hensley
 * January 18, 2010
 * This class forms the base for the other rating methods. It
 * applies no adjusts to the point different and does not
 * account for any home field advantage.
 */
using System.Collections.Generic;
using System.Linq;
using DatabaseLayer;

namespace RatingSystem
{
    public enum RatingObject
    {
        Team = 0,
        Conference = 1,
        Division = 2
    }

    public class StandardRating
    {
        public static string RatingName = "Standard Ratings";
        protected Dictionary<int, int> teamIDMapping;
        protected Dictionary<int, int> conferenceIDMapping;
        protected Dictionary<int, int> divisionIDMapping;
        protected CollegeFootballEntities entities;
        protected int teamCount;
        protected int conferenceCount;
        protected int divisionCount;
        protected RatingSettings ratingSettings;
        protected int group;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        /// <param name="_group">The group to calculate ratings for</param>
        public StandardRating(CollegeFootballEntities _entities, int _group)
        {
            entities = _entities;
            group = _group;
            ratingSettings = new RatingSettings();
            CreateTeamIDMapping();
            CreateConferenceIDMapping();
            CreateDivisionIDMapping();
        }

        public StandardRating(CollegeFootballEntities _entities, int _group, RatingSettings _settings)
        {
            entities = _entities;
            group = _group;
            ratingSettings = _settings;
            CreateTeamIDMapping();
            CreateConferenceIDMapping();
            CreateDivisionIDMapping();
        }

        /// <summary>
        /// Calls GaussJordanElimination to solve the matrix and then maps the
        /// results back to their team, conference, or division IDs.
        /// </summary>
        /// <param name="matrix">The matrix to solve</param>
        /// <param name="ratingObject">
        /// Indicates the type of object the rating applies to
        /// </param>
        /// <returns>A dictionary of IDs to ratings</returns>
        public virtual Dictionary<int, double> GetRatings(double[][] matrix,
                                                           RatingObject ratingObject)
        {
            Dictionary<int, double> ratingDictionary = new Dictionary<int, double>();
            GaussJordanElimination gj = new GaussJordanElimination();
            double[] ratingVector;
            int rowCount;

            rowCount = matrix.Count();
            ratingVector = gj.PerformElimination(matrix, rowCount, rowCount + 1);

            for (int row = 0; row < rowCount; row++)
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

        protected IList<Game> GetTeamHomeGames(Team team)
            => team.HomeGames.Where(g => g.Year == ratingSettings.Year && g.Date <= ratingSettings.CurrentCutoffDate).ToList();

        protected IList<Game> GetTeamAwayGames(Team team)
            => team.AwayGames.Where(g => g.Year == ratingSettings.Year && g.Date <= ratingSettings.CurrentCutoffDate).ToList();

        /// <summary>
        /// Creates a mapping of team IDs to matrix row numbers
        /// </summary>
        protected void CreateTeamIDMapping()
        {
            teamIDMapping = new Dictionary<int, int>();
            teamCount = 0;

            foreach (Team team in entities.Teams.Where(t => t.Group ==
                                            group).OrderBy(t => t.ID))
            {
                teamIDMapping.Add(team.ID, teamCount);
                teamCount++;
            }
        }

        /// <summary>
        /// Creates a mapping of conference IDs to matrix row numbers
        /// </summary>
        protected void CreateConferenceIDMapping()
        {
            conferenceIDMapping = new Dictionary<int, int>();
            conferenceCount = 0;

            foreach (Conference conference in entities.Conferences.Where(c =>
                            c.Group == group).OrderBy(d => d.ID))
            {
                conferenceIDMapping.Add(conference.ID, conferenceCount);
                conferenceCount++;
            }
        }

        /// <summary>
        /// Creates a mapping of division IDs to matrix row numbers
        /// </summary>
        protected void CreateDivisionIDMapping()
        {
            divisionIDMapping = new Dictionary<int, int>();
            divisionCount = 0;

            foreach (Division division in entities.Divisions.Where(d => d.Group ==
                            group).OrderBy(d => d.ID))
            {
                divisionIDMapping.Add(division.ID, divisionCount);
                divisionCount++;
            }
        }

        /// <summary>
        /// Creates the team matrix to solve
        /// </summary>
        /// <returns>The team matrix</returns>
        public virtual double[][] CreateTeamMatrix()
        {
            int rowCount = 0;
            double[][] matrix = new double[teamCount][];

            foreach (Team team in entities.Teams.Where(t => t.Group ==
                            group).OrderBy(t => t.ID))
            {
                var homeGames = GetTeamHomeGames(team);
                var awayGames = GetTeamAwayGames(team);
                int gameCount = homeGames.Count + awayGames.Count;
                double pd = homeGames.Sum(g => g.HomeScore - g.AwayScore)
                          + awayGames.Sum(g => g.AwayScore - g.HomeScore);

                matrix[rowCount] = new double[teamCount + 1];
                matrix[teamIDMapping[team.ID]][teamIDMapping[team.ID]] = gameCount;
                matrix[rowCount][teamCount] = pd;
                rowCount++;
            }

            foreach (Game game in entities.GetGames(group, ratingSettings.Year, ratingSettings.CurrentCutoffDate))
            {
                matrix[teamIDMapping[game.HomeTeamID]][teamIDMapping[game.AwayTeamID]] -= 1;
                matrix[teamIDMapping[game.AwayTeamID]][teamIDMapping[game.HomeTeamID]] -= 1;
            }

            for (int col = 0; col < teamCount; col++)
                matrix[teamCount - 1][col] = 1;
            matrix[teamCount - 1][teamCount] = 0;

            return matrix;
        }

        /// <summary>
        /// Creates the conference matrix to solve
        /// </summary>
        /// <param name="interConferenceGames">
        /// The games between conferences in the group
        /// </param>
        /// <returns>The conference matrix</returns>
        public virtual double[][] CreateConferenceMatrix(
                        IEnumerable<Game> interConferenceGames)
        {
            int rowCount = 0;
            int gameCount = 0;
            double[][] matrix = new double[conferenceCount][];
            IEnumerable<Game> conferenceGames;

            foreach (Conference conference in entities.Conferences.Where(c =>
                            c.Group == group).OrderBy(c => c.ID))
            {
                conferenceGames = entities.GetGames(conference, ratingSettings.Year, ratingSettings.CurrentCutoffDate);
                gameCount = conferenceGames.Count();

                matrix[rowCount] = new double[conferenceCount + 1];

                // Diaganol value is the number of games played
                matrix[conferenceIDMapping[conference.ID]]
                      [conferenceIDMapping[conference.ID]] = gameCount;

                // Right hand side of every line is total score margin
                matrix[rowCount][conferenceCount] =
                        entities.GetPointDifferential(conferenceGames, conference);
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
        /// Creates the division matrix to solve
        /// </summary>
        /// <param name="interDivisionGames">
        /// The games between divisions in the group
        /// </param>
        /// <returns>The division matrix</returns>
        public virtual double[][] CreateDivisionMatrix(
                        IEnumerable<Game> interDivisionGames)
        {
            int rowCount = 0;
            int gameCount = 0;
            double[][] matrix = new double[divisionCount][];
            IEnumerable<Game> divisionGames;

            foreach (Division division in entities.Divisions.Where(d => d.Group ==
                            group).OrderBy(d => d.ID))
            {
                divisionGames = entities.GetGames(division, ratingSettings.Year, ratingSettings.CurrentCutoffDate);
                gameCount = divisionGames.Count();

                matrix[rowCount] = new double[divisionCount + 1];

                // Diaganol value is the number of games played
                matrix[divisionIDMapping[division.ID]]
                      [divisionIDMapping[division.ID]] = gameCount;

                // Right hand side of every line is total score margin
                matrix[rowCount][divisionCount] =
                        entities.GetPointDifferential(divisionGames, division);
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
