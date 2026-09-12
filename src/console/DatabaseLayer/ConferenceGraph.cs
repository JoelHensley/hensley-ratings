using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer
{
    public class ConferenceGraph
    {
        private CollegeFootballEntities entities = null;
        private List<int> visitedGroups = null;

        public ConferenceGraph(CollegeFootballEntities _entities)
        {
            entities = _entities;
            visitedGroups = new List<int>();
        }

        public void CreateConferenceGroups()
        {
            Conference conference;
            int group = 1;

            conference = entities.Conferences.FirstOrDefault(c => c.Group == null);
            while (conference != null)
            {
                visitedGroups.Clear();
                ConnectConferences(conference, group);
                conference = entities.Conferences.FirstOrDefault(t => t.Group == null);
                group++;
            }
        }

        private void ConnectConferences(Conference conference, int group)
        {
            List<Conference> groupConferences;
            List<int?> groupList;
            conference.Group = group;
            entities.SaveChanges();

            groupList = conference.Teams.Where(t => t.Group != null).Select(t =>
                                t.Group).Distinct().ToList();

            foreach (int teamGroup in groupList)
            {
                if (visitedGroups.Contains(teamGroup))
                    continue;

                visitedGroups.Add(teamGroup);

                groupConferences = (from t in entities.Teams
                        join c in entities.Conferences on t.ConferenceID equals c.ID
                        where t.Group == teamGroup
                        && c.Group == null
                        select c).ToList();

                foreach (Conference otherConference in groupConferences)
                {
                    entities.Entry(otherConference).Reload();
                    if (otherConference.Group == null)
                        ConnectConferences(otherConference, group);
                }
            }
        }

        public static void AssignGroups(IList<Conference> conferences, IList<Team> teams)
        {
            int group = 1;
            foreach (var conference in conferences)
            {
                if (conference.Group == null)
                {
                    var visited = new HashSet<int>();
                    ConnectConferencesInMemory(conference, group, conferences, teams, visited);
                    group++;
                }
            }
        }

        private static void ConnectConferencesInMemory(Conference conf, int group,
            IList<Conference> conferences, IList<Team> teams, HashSet<int> visited)
        {
            conf.Group = group;

            var teamGroups = teams
                .Where(t => t.ConferenceID == conf.ID && t.Group.HasValue)
                .Select(t => t.Group.Value)
                .Distinct()
                .ToList();

            foreach (int teamGroup in teamGroups)
            {
                if (!visited.Add(teamGroup))
                    continue;

                var otherConfs = conferences
                    .Where(c => c.Group == null
                             && teams.Any(t => t.Group == teamGroup && t.ConferenceID == c.ID))
                    .ToList();

                foreach (var other in otherConfs)
                {
                    if (other.Group == null)
                        ConnectConferencesInMemory(other, group, conferences, teams, visited);
                }
            }
        }
    }
}
