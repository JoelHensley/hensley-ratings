using System.Linq;
using DatabaseLayer;

namespace DataImport
{
    class TeamGraph
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

            // Only traverse edges from games in the current season
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
    }
}
