using System.Collections.Generic;
using System.Linq;

namespace DatabaseLayer
{
    public class TeamGraph
    {
        private CollegeFootballEntities entities = null;

        public TeamGraph(CollegeFootballEntities _entities)
        {
            entities = _entities;
        }

        public void CreateTeamGroups(int year)
        {
            Team team;
            int group = 1;

            team = entities.Teams.FirstOrDefault(t => t.Group == null);
            while (team != null)
            {
                ConnectTeams(team, group, year);
                team = entities.Teams.FirstOrDefault(t => t.Group == null);
                group++;
            }
        }

        private void ConnectTeams(Team team, int group, int year)
        {
            team.Group = group;
            entities.SaveChanges();

            var oppIds = entities.Games
                .Where(g => g.Year == year
                    && (g.HomeTeamID == team.ID || g.AwayTeamID == team.ID))
                .Select(g => g.HomeTeamID == team.ID ? g.AwayTeamID : g.HomeTeamID)
                .Distinct()
                .ToList();

            foreach (int oppId in oppIds)
            {
                var opp = entities.Teams.FirstOrDefault(t => t.ID == oppId && t.Group == null);
                if (opp != null)
                    ConnectTeams(opp, group, year);
            }
        }

        public static void AssignGroups(IList<Team> teams, IList<Game> weekGames)
        {
            int group = 1;
            foreach (var team in teams)
            {
                if (team.Group == null)
                {
                    BfsConnect(team, group, teams, weekGames);
                    group++;
                }
            }
        }

        private static void BfsConnect(Team start, int group, IList<Team> teams, IList<Game> games)
        {
            var queue = new Queue<Team>();
            start.Group = group;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var oppIds = games
                    .Where(g => g.HomeTeamID == current.ID || g.AwayTeamID == current.ID)
                    .Select(g => g.HomeTeamID == current.ID ? g.AwayTeamID : g.HomeTeamID)
                    .Distinct();
                foreach (var oppId in oppIds)
                {
                    var opp = teams.FirstOrDefault(t => t.ID == oppId && t.Group == null);
                    if (opp != null)
                    {
                        opp.Group = group;
                        queue.Enqueue(opp);
                    }
                }
            }
        }
    }
}
