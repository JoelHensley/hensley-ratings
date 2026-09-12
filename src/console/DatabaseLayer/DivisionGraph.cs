using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer
{
    public class DivisionGraph
    {
        private CollegeFootballEntities entities = null;
        private List<int> visitedGroups = null;

        public DivisionGraph(CollegeFootballEntities _entities)
        {
            entities = _entities;
            visitedGroups = new List<int>();
        }

        public void CreateDivisionGroups()
        {
            Division division;
            int group = 1;

            division = entities.Divisions.FirstOrDefault(d => d.Group == null);
            while (division != null)
            {
                visitedGroups.Clear();
                ConnectDivisions(division, group);
                division = entities.Divisions.FirstOrDefault(t => t.Group == null);
                group++;
            }
        }

        private void ConnectDivisions(Division division, int group)
        {
            List<Division> groupDivisions;
            List<int?> groupList;
            division.Group = group;
            entities.SaveChanges();

            groupList = (from t in entities.Teams
                         join c in entities.Conferences on t.ConferenceID equals c.ID
                         where c.DivisionID == division.ID
                         && t.Group != null
                         select t.Group).Distinct().ToList();

            foreach (int teamGroup in groupList)
            {
                if (visitedGroups.Contains(teamGroup))
                    continue;

                visitedGroups.Add(teamGroup);

                groupDivisions = (from t in entities.Teams
                         join c in entities.Conferences on t.ConferenceID equals c.ID
                         join d in entities.Divisions on c.DivisionID equals d.ID
                         where t.Group == teamGroup
                         && d.Group == null
                         select d).ToList();

                foreach (Division otherDivision in groupDivisions)
                {
                    entities.Entry(otherDivision).Reload();
                    if (otherDivision.Group == null)
                        ConnectDivisions(otherDivision, group);
                }
            }
        }

        public static void AssignGroups(IList<Division> divisions, IList<Conference> conferences, IList<Team> teams)
        {
            int group = 1;
            foreach (var division in divisions)
            {
                if (division.Group == null)
                {
                    var visited = new HashSet<int>();
                    ConnectDivisionsInMemory(division, group, divisions, conferences, teams, visited);
                    group++;
                }
            }
        }

        private static void ConnectDivisionsInMemory(Division div, int group,
            IList<Division> divisions, IList<Conference> conferences, IList<Team> teams,
            HashSet<int> visited)
        {
            div.Group = group;

            var teamGroups = teams
                .Where(t => t.Group.HasValue)
                .Where(t => {
                    var conf = conferences.FirstOrDefault(c => c.ID == t.ConferenceID);
                    return conf != null && conf.DivisionID == div.ID;
                })
                .Select(t => t.Group.Value)
                .Distinct()
                .ToList();

            foreach (int teamGroup in teamGroups)
            {
                if (!visited.Add(teamGroup))
                    continue;

                var otherDivIds = teams
                    .Where(t => t.Group == teamGroup)
                    .Select(t => conferences.FirstOrDefault(c => c.ID == t.ConferenceID)?.DivisionID)
                    .Where(did => did.HasValue && did.Value != div.ID)
                    .Select(did => did.Value)
                    .Distinct()
                    .ToList();

                foreach (var otherId in otherDivIds)
                {
                    var other = divisions.FirstOrDefault(d => d.ID == otherId && d.Group == null);
                    if (other != null)
                        ConnectDivisionsInMemory(other, group, divisions, conferences, teams, visited);
                }
            }
        }
    }
}
