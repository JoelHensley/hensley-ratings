/* CollegeFootballEntities.cs
 * Joel Hensley
 * January 13, 2010
 * This class is used to add addtional methods and properties
 * to the CollegeFootballEntities object.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseLayer
{
    public partial class CollegeFootballEntities
    {
        /// <summary>
        /// Gets all the games played between different conferences
        /// </summary>
        /// <param name="conferenceGroup">The group of conferences being examined</param>
        /// <returns>The interconference games</returns>
        public IEnumerable<Game> GetInterConferenceGames(int conferenceGroup)
        {
            return from g in Games
                   join ht in Teams on g.HomeTeamID equals ht.ID
                   join at in Teams on g.AwayTeamID equals at.ID
                   join hc in Conferences on ht.ConferenceID equals hc.ID
                   where ht.ConferenceID != at.ConferenceID
                       && hc.Group == conferenceGroup
                   select g;
        }

        public IEnumerable<Game> GetInterConferenceGames(int conferenceGroup, int year)
        {
            return from g in Games
                   join ht in Teams on g.HomeTeamID equals ht.ID
                   join at in Teams on g.AwayTeamID equals at.ID
                   join hc in Conferences on ht.ConferenceID equals hc.ID
                   where ht.ConferenceID != at.ConferenceID
                       && hc.Group == conferenceGroup
                       && g.Year == year
                   select g;
        }

        /// <summary>
        /// Gets all the games played between different divisions
        /// </summary>
        /// <param name="divisionGroup">The group of divisions being examined</param>
        /// <returns>The interdivision games</returns>
        public IEnumerable<Game> GetInterDivisionGames(int divisionGroup)
        {
            return from g in Games
                   join ht in Teams on g.HomeTeamID equals ht.ID
                   join at in Teams on g.AwayTeamID equals at.ID
                   join hc in Conferences on ht.ConferenceID equals hc.ID
                   join ac in Conferences on at.ConferenceID equals ac.ID
                   join hd in Divisions on hc.DivisionID equals hd.ID
                   where hc.DivisionID != ac.DivisionID
                       && hd.Group == divisionGroup
                   select g;
        }

        public IEnumerable<Game> GetInterDivisionGames(int divisionGroup, int year)
        {
            return from g in Games
                   join ht in Teams on g.HomeTeamID equals ht.ID
                   join at in Teams on g.AwayTeamID equals at.ID
                   join hc in Conferences on ht.ConferenceID equals hc.ID
                   join ac in Conferences on at.ConferenceID equals ac.ID
                   join hd in Divisions on hc.DivisionID equals hd.ID
                   where hc.DivisionID != ac.DivisionID
                       && hd.Group == divisionGroup
                       && g.Year == year
                   select g;
        }

        /// <summary>
        /// Gets the number of wins for a given conference in a given set of games
        /// </summary>
        /// <param name="interConferenceGames">The set of interconference games</param>
        /// <param name="conference">The conference being examined</param>
        /// <returns>The total number of wins</returns>
        public int GetWins(IEnumerable<Game> interConferenceGames, Conference conference)
        {
            int wins;

            wins = (from g in interConferenceGames
                    join ht in Teams on g.HomeTeamID equals ht.ID
                    join at in Teams on g.AwayTeamID equals at.ID
                    where (ht.ConferenceID == conference.ID && g.HomeScore > g.AwayScore)
                        || (at.ConferenceID == conference.ID && g.AwayScore > g.HomeScore)
                    select g.ID).Count();

            return wins;
        }

        /// <summary>
        /// Gets the number of losses for a given conference in a given set of games
        /// </summary>
        /// <param name="interConferenceGames">The set of interconference games</param>
        /// <param name="conference">The conference being examined</param>
        /// <returns>The total number of losses</returns>
        public int GetLosses(IEnumerable<Game> interConferenceGames, Conference conference)
        {
            int losses;

            losses = (from g in interConferenceGames
                    join ht in Teams on g.HomeTeamID equals ht.ID
                    join at in Teams on g.AwayTeamID equals at.ID
                    where (ht.ConferenceID == conference.ID && g.HomeScore < g.AwayScore)
                        || (at.ConferenceID == conference.ID && g.AwayScore < g.HomeScore)
                    select g.ID).Count();

            return losses;
        }

        /// <summary>
        /// Gets the number of wins for a given division in a given set of games
        /// </summary>
        /// <param name="interDivisionGames">The set of interdivision games</param>
        /// <param name="division">The division being examined</param>
        /// <returns>The total number of wins</returns>
        public int GetWins(IEnumerable<Game> interDivisionGames, Division division)
        {
            int wins;

            wins = (from g in interDivisionGames
                    join ht in Teams on g.HomeTeamID equals ht.ID
                    join at in Teams on g.AwayTeamID equals at.ID
                    join hc in Conferences on ht.ConferenceID equals hc.ID
                    join ac in Conferences on at.ConferenceID equals ac.ID
                    where (hc.DivisionID == division.ID && g.HomeScore > g.AwayScore)
                        || (ac.DivisionID == division.ID && g.AwayScore > g.HomeScore)
                    select g.ID).Count();

            return wins;
        }

        /// <summary>
        /// Gets the number of losses for a given division in a given set of games
        /// </summary>
        /// <param name="interDivisionGames">The set of interdivision games</param>
        /// <param name="division">The division being examined</param>
        /// <returns>The total number of losses</returns>
        public int GetLosses(IEnumerable<Game> interDivisionGames, Division division)
        {
            int losses;

            losses = (from g in interDivisionGames
                    join ht in Teams on g.HomeTeamID equals ht.ID
                    join at in Teams on g.AwayTeamID equals at.ID
                    join hc in Conferences on ht.ConferenceID equals hc.ID
                    join ac in Conferences on at.ConferenceID equals ac.ID
                    where (hc.DivisionID == division.ID && g.HomeScore < g.AwayScore)
                        || (ac.DivisionID == division.ID && g.AwayScore < g.HomeScore)
                    select g.ID).Count();

            return losses;
        }

        /// <summary>
        /// Gets the total point differential for a conference in a given set of games
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="conference">The conference</param>
        /// <returns>The total point differential</returns>
        public int GetPointDifferential(IEnumerable<Game> games, Conference conference)
        {
            return GetPointDifferential(games, conference, 0);
        }

        /// <summary>
        /// Gets the total point differential adjusted for the maximum point differential
        /// per game for a conference in a given set of games
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="conference">The conference</param>
        /// <param name="maxPointDifferential">The maximum point differential</param>
        /// <returns>The total point differential</returns>
        /// <remarks>
        /// If the maximum point differential is less than or equal to zero, then
        /// no adjustment is made to the total point differential.
        /// </remarks>
        public int GetPointDifferential(IEnumerable<Game> games, Conference conference,
                                        int maxPointDifferential)
        {
            int gameDifferential;
            int calculatedPointDifferential = 0;

            var homeGameScores = from g in games
                                 join ht in Teams on g.HomeTeamID equals ht.ID
                                 where ht.ConferenceID == conference.ID
                                 select new { g.HomeScore, g.AwayScore };
            var awayGameScores = from g in games
                                 join at in Teams on g.AwayTeamID equals at.ID
                                 where at.ConferenceID == conference.ID
                                 select new { g.HomeScore, g.AwayScore };

            if (maxPointDifferential > 0)
            {
                foreach (var homeGameScore in homeGameScores)
                {
                    gameDifferential = homeGameScore.HomeScore - homeGameScore.AwayScore;
                    gameDifferential = (Math.Abs(gameDifferential) > maxPointDifferential)
                                ? maxPointDifferential * Math.Sign(gameDifferential)
                                : gameDifferential;
                    calculatedPointDifferential += gameDifferential;
                }

                foreach (var awayGameScore in awayGameScores)
                {
                    gameDifferential = awayGameScore.AwayScore - awayGameScore.HomeScore;
                    gameDifferential = (Math.Abs(gameDifferential) > maxPointDifferential)
                                ? maxPointDifferential * Math.Sign(gameDifferential)
                                : gameDifferential;
                    calculatedPointDifferential += gameDifferential;
                }
            }
            else
            {
                calculatedPointDifferential = (homeGameScores.Sum(h => h.HomeScore)
                                             + awayGameScores.Sum(h => h.AwayScore))
                                           - (homeGameScores.Sum(h => h.AwayScore)
                                             + awayGameScores.Sum(h => h.HomeScore));
            }

            return calculatedPointDifferential;
        }

        /// <summary>
        /// Gets the total computed win-loss value for a conference in a given
        /// set of games using a specified upper and lower bounds.
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="conference">The conference</param>
        /// <param name="lowerBounds">The lower bounds to the win-loss value</param>
        /// <param name="upperBounds">The upper bounds to the win-loss value</param>
        /// <returns>The total computed win-loss value</returns>
        public double GetHensleyPointDifferential(IEnumerable<Game> games,
                Conference conference, double lowerBounds, double upperBounds)
        {
            bool isWinner;
            double winnerScore;
            double loserScore;
            double computedDifferentialScore;
            double cumulativeTotal = 0;

            var homeGameScores = from g in games
                                 join ht in Teams on g.HomeTeamID equals ht.ID
                                 where ht.ConferenceID == conference.ID
                                 select new { g.HomeScore, g.AwayScore };
            var awayGameScores = from g in games
                                 join at in Teams on g.AwayTeamID equals at.ID
                                 where at.ConferenceID == conference.ID
                                 select new { g.HomeScore, g.AwayScore };

            foreach (var gameScore in homeGameScores)
            {
                isWinner = (gameScore.HomeScore > gameScore.AwayScore);
                if (isWinner)
                {
                    winnerScore = (double)gameScore.HomeScore;
                    loserScore = (double)gameScore.AwayScore;
                }
                else
                {
                    winnerScore = (double)gameScore.AwayScore;
                    loserScore = (double)gameScore.HomeScore;
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

            foreach (var gameScore in awayGameScores)
            {
                isWinner = (gameScore.AwayScore > gameScore.HomeScore);
                if (isWinner)
                {
                    winnerScore = (double)gameScore.AwayScore;
                    loserScore = (double)gameScore.HomeScore;
                }
                else
                {
                    winnerScore = (double)gameScore.HomeScore;
                    loserScore = (double)gameScore.AwayScore;
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

        /// <summary>
        /// Gets the total point differential for a division in a given set of games
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="division">The division</param>
        /// <returns>The total point differential</returns>
        public int GetPointDifferential(IEnumerable<Game> games, Division division)
        {
            return GetPointDifferential(games, division, 0);
        }

        /// <summary>
        /// Gets the total point differential adjusted for the maximum point differential
        /// per game for a division in a given set of games
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="division">The division</param>
        /// <param name="maxPointDifferential">The maximum point differential</param>
        /// <returns>The total point differential</returns>
        /// <remarks>
        /// If the maximum point differential is less than or equal to zero, then
        /// no adjustment is made to the total point differential.
        /// </remarks>
        public int GetPointDifferential(IEnumerable<Game> games, Division division,
                                        int maxPointDifferential)
        {
            int gameDifferential;
            int calculatedPointDifferential = 0;

            var homeGameScores = from g in games
                                 join ht in Teams on g.HomeTeamID equals ht.ID
                                 join hc in Conferences on ht.ConferenceID equals hc.ID
                                 where hc.DivisionID == division.ID
                                 select new { g.HomeScore, g.AwayScore };
            var awayGameScores = from g in games
                                 join at in Teams on g.AwayTeamID equals at.ID
                                 join ac in Conferences on at.ConferenceID equals ac.ID
                                 where ac.DivisionID == division.ID
                                 select new { g.HomeScore, g.AwayScore };

            if (maxPointDifferential > 0)
            {
                foreach (var homeGameScore in homeGameScores)
                {
                    gameDifferential = homeGameScore.HomeScore - homeGameScore.AwayScore;
                    gameDifferential = (Math.Abs(gameDifferential) > maxPointDifferential)
                                ? maxPointDifferential * Math.Sign(gameDifferential)
                                : gameDifferential;
                    calculatedPointDifferential += gameDifferential;
                }

                foreach (var awayGameScore in awayGameScores)
                {
                    gameDifferential = awayGameScore.AwayScore - awayGameScore.HomeScore;
                    gameDifferential = (Math.Abs(gameDifferential) > maxPointDifferential)
                                ? maxPointDifferential * Math.Sign(gameDifferential)
                                : gameDifferential;
                    calculatedPointDifferential += gameDifferential;
                }
            }
            else
            {
                calculatedPointDifferential = (homeGameScores.Sum(h => h.HomeScore)
                                             + awayGameScores.Sum(h => h.AwayScore))
                                           - (homeGameScores.Sum(h => h.AwayScore)
                                             + awayGameScores.Sum(h => h.HomeScore));
            }

            return calculatedPointDifferential;
        }

        /// <summary>
        /// Gets the total computed win-loss value for a division in a given
        /// set of games using a specified upper and lower bounds.
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="division">The division</param>
        /// <param name="lowerBounds">The lower bounds to the win-loss value</param>
        /// <param name="upperBounds">The upper bounds to the win-loss value</param>
        /// <returns>The total computed win-loss value</returns>
        public double GetHensleyPointDifferential(IEnumerable<Game> games,
                Division division, double lowerBounds, double upperBounds)
        {
            bool isWinner;
            double winnerScore;
            double loserScore;
            double computedDifferentialScore;
            double cumulativeTotal = 0;

            var homeGameScores = from g in games
                                 join ht in Teams on g.HomeTeamID equals ht.ID
                                 join hc in Conferences on ht.ConferenceID equals hc.ID
                                 where hc.DivisionID == division.ID
                                 select new { g.HomeScore, g.AwayScore };
            var awayGameScores = from g in games
                                 join at in Teams on g.AwayTeamID equals at.ID
                                 join ac in Conferences on at.ConferenceID equals ac.ID
                                 where ac.DivisionID == division.ID
                                 select new { g.HomeScore, g.AwayScore };

            foreach (var gameScore in homeGameScores)
            {
                isWinner = (gameScore.HomeScore > gameScore.AwayScore);
                if (isWinner)
                {
                    winnerScore = (double)gameScore.HomeScore;
                    loserScore = (double)gameScore.AwayScore;
                }
                else
                {
                    winnerScore = (double)gameScore.AwayScore;
                    loserScore = (double)gameScore.HomeScore;
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

            foreach (var gameScore in awayGameScores)
            {
                isWinner = (gameScore.AwayScore > gameScore.HomeScore);
                if (isWinner)
                {
                    winnerScore = (double)gameScore.AwayScore;
                    loserScore = (double)gameScore.HomeScore;
                }
                else
                {
                    winnerScore = (double)gameScore.HomeScore;
                    loserScore = (double)gameScore.AwayScore;
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

        /// <summary>
        /// Gets the total number of home games for a conference minues the
        /// total number of away games for a conference.
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="conference">The conference</param>
        /// <returns>The home game differential</returns>
        public int GetHomeGameDifferential(IEnumerable<Game> games, Conference conference)
        {
            int homeGameCount = (from g in games
                                 join ht in Teams on g.HomeTeamID equals ht.ID
                                 where ht.ConferenceID == conference.ID
                                 && g.IsNeutralSite == false
                                 select g.ID).Count();
            int awayGameCount = (from g in games
                                 join at in Teams on g.AwayTeamID equals at.ID
                                 where at.ConferenceID == conference.ID
                                 && g.IsNeutralSite == false
                                 select g.ID).Count();

            return homeGameCount - awayGameCount;
        }

        /// <summary>
        /// Gets the total number of home games for a division minues the
        /// total number of away games for a division.
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="division">The division</param>
        /// <returns>The home game differential</returns>
        public int GetHomeGameDifferential(IEnumerable<Game> games, Division division)
        {
            int homeGameCount = (from g in games
                                 join ht in Teams on g.HomeTeamID equals ht.ID
                                 join hc in Conferences on ht.ConferenceID equals hc.ID
                                 where hc.DivisionID == division.ID
                                 && g.IsNeutralSite == false
                                 select g.ID).Count();
            int awayGameCount = (from g in games
                                 join at in Teams on g.AwayTeamID equals at.ID
                                 join ac in Conferences on at.ConferenceID equals ac.ID
                                 where ac.DivisionID == division.ID
                                 && g.IsNeutralSite == false
                                 select g.ID).Count();

            return homeGameCount - awayGameCount;
        }

        /// <summary>
        /// Gets all the games for all the teams in a conference
        /// </summary>
        /// <param name="conference">The conference</param>
        /// <returns>All games played by that conference</returns>
        public IEnumerable<Game> GetGames(Conference conference)
        {
            IEnumerable<Game> conferenceGames = null;

            conferenceGames = from g in Games
                              join ht in Teams on g.HomeTeamID equals ht.ID
                              join at in Teams on g.AwayTeamID equals at.ID
                              where (ht.ConferenceID == conference.ID
                                     && at.ConferenceID != conference.ID)
                              || (ht.ConferenceID != conference.ID
                                     && at.ConferenceID == conference.ID)
                              select g;

            return conferenceGames;
        }

        /// <summary>
        /// Gets all the games for all the teams in a division
        /// </summary>
        /// <param name="division">The division</param>
        /// <returns>All games played by that division</returns>
        public IEnumerable<Game> GetGames(Division division)
        {
            IEnumerable<Game> divisionGames = null;

            divisionGames = from g in Games
                            join ht in Teams on g.HomeTeamID equals ht.ID
                            join at in Teams on g.AwayTeamID equals at.ID
                            join hc in Conferences on ht.ConferenceID equals hc.ID
                            join ac in Conferences on at.ConferenceID equals ac.ID
                            where (hc.DivisionID == division.ID
                                   && ac.DivisionID != division.ID)
                            || (hc.DivisionID != division.ID
                                   && ac.DivisionID == division.ID)
                            select g;

            return divisionGames;
        }

        /// <summary>
        /// Gets all the games played by teams in a given group
        /// </summary>
        /// <param name="groupNum">The group number</param>
        /// <returns>All games played by that team group</returns>
        public IEnumerable<Game> GetGames(int groupNum)
        {
            return from g in Games
                   join ht in Teams on g.HomeTeamID equals ht.ID
                   where ht.Group == groupNum
                   select g;
        }

        public IEnumerable<Game> GetGames(int groupNum, int year)
        {
            return from g in Games
                   join ht in Teams on g.HomeTeamID equals ht.ID
                   where ht.Group == groupNum && g.Year == year
                   select g;
        }

        /// <summary>
        /// Gets the total number of home games in a set of games
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <returns>The total number of home games</returns>
        public int GetHomeGameCount(IEnumerable<Game> games)
        {
            int conferenceHomeGameCount = (from g in games
                                           where g.IsNeutralSite == false
                                           select g.ID).Count();

            return conferenceHomeGameCount;
        }

        /// <summary>
        /// Gets the total point differential of all home games in a given
        /// set of games
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <returns>The total point differential</returns>
        public int GetHomeGamePointDifferential(IEnumerable<Game> games)
        {
            var homeGames = from g in games
                            where g.IsNeutralSite == false
                            select new { g.HomeScore, g.AwayScore };

            return homeGames.Sum(g => g.HomeScore)
                   - homeGames.Sum(g => g.AwayScore);
        }

        /// <summary>
        /// Gets the total computed win-loss value of all home games in a given
        /// set of games using a specified upper and lower bounds.
        /// </summary>
        /// <param name="games">The set of games</param>
        /// <param name="lowerBounds">The lower bounds to the win-loss value</param>
        /// <param name="upperBounds">The upper bounds to the win-loss value</param>
        /// <returns>The total computed win-loss value</returns>
        public double GetHomeGameHensleyPointDifferential(IEnumerable<Game> games,
                                        double lowerBounds, double upperBounds)
        {
            bool isWinner;
            double winnerScore;
            double loserScore;
            double computedDifferentialScore;
            double cumulativeTotal = 0;

            var homeGames = from g in games
                            where g.IsNeutralSite == false
                            select new { g.HomeScore, g.AwayScore };

            foreach (var gameScore in homeGames)
            {
                isWinner = (gameScore.HomeScore > gameScore.AwayScore);
                if (isWinner)
                {
                    winnerScore = (double)gameScore.HomeScore;
                    loserScore = (double)gameScore.AwayScore;
                }
                else
                {
                    winnerScore = (double)gameScore.AwayScore;
                    loserScore = (double)gameScore.HomeScore;
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

        /// <summary>
        /// Deletes all rows from all tables in the database.
        /// </summary>
        public void DeleteAllRows()
        {
            TeamResults.RemoveRange(TeamResults.ToList());
            ConferenceResults.RemoveRange(ConferenceResults.ToList());
            DivisionResults.RemoveRange(DivisionResults.ToList());
            Games.RemoveRange(Games.ToList());
            TeamAffiliations.RemoveRange(TeamAffiliations.ToList());
            ConferenceAffiliations.RemoveRange(ConferenceAffiliations.ToList());
            WeekSettings.RemoveRange(WeekSettings.ToList());
            Teams.RemoveRange(Teams.ToList());
            Conferences.RemoveRange(Conferences.ToList());
            Divisions.RemoveRange(Divisions.ToList());
            SaveChanges();
        }
    }
}
