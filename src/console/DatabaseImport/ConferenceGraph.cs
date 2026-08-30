/* ConferenceGraph.cs
 * Joel Hensley
 * February 9, 2010
 * This class is used to identify and store the different conference
 * groups in the database. A conference is connected to another conference
 * if there is a series of games for any of the teams in that conference
 * that connects to any of the teams in another conference.
 */
using System.Collections.Generic;
using System.Linq;
using DatabaseLayer;
using Microsoft.EntityFrameworkCore;

namespace DataImport
{
    class ConferenceGraph
    {
        private CollegeFootballEntities entities = null;
        private List<int> visitedGroups = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        public ConferenceGraph(CollegeFootballEntities _entities)
        {
            entities = _entities;
            visitedGroups = new List<int>();
        }

        /// <summary>
        /// Loops through all the conferences in the database until all
        /// conferences are included in some group.
        /// </summary>
        public void CreateConferenceGroups()
        {
            Conference conference;
            int group = 1;

            conference = entities.Conferences.FirstOrDefault(c => c.Group == null);
            while (conference != null)
            {
                visitedGroups.Clear();
                ConnectConferences(conference, group);
                conference = entities.Conferences.FirstOrDefault(t => t.Group ==
                                                                        null);
                group++;
            }
        }

        /// <summary>
        /// Recursively loops through a given conference's distinct set of team
        /// groups to find other conferences with at least one team in that group.
        /// </summary>
        /// <param name="team">The conference being examined</param>
        /// <param name="group">The group the conference belongs to</param>
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
                {
                    // We've already visited that group
                    continue;
                }

                visitedGroups.Add(teamGroup);

                groupConferences = (from t in entities.Teams
                        join c in entities.Conferences on t.ConferenceID equals c.ID
                        where t.Group == teamGroup
                        && c.Group == null
                        select c).ToList();

                foreach (Conference otherConference in groupConferences)
                {
                    // It's possible the conference has already been examined
                    // so we don't want to call the recursive method if so.
                    entities.Entry(otherConference).Reload();

                    if (otherConference.Group == null)
                    {
                        ConnectConferences(otherConference, group);
                    }
                }
            }
        }
    }
}
