/* DivisionGraph.cs
 * Joel Hensley
 * February 9, 2010
 * This class is used to identify and store the different division
 * groups in the database. A division is connected to another division
 * if there is a series of games for any of the teams in that division
 * that connects to any of the teams in another division.
 */
using System.Collections.Generic;
using System.Linq;
using DatabaseLayer;
using Microsoft.EntityFrameworkCore;

namespace DataImport
{
    class DivisionGraph
    {
        private CollegeFootballEntities entities = null;
        private List<int> visitedGroups = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        public DivisionGraph(CollegeFootballEntities _entities)
        {
            entities = _entities;
            visitedGroups = new List<int>();
        }

        /// <summary>
        /// Loops through all the divisions in the database until all
        /// divisions are included in some group.
        /// </summary>
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

        /// <summary>
        /// Recursively loops through a given division's distinct set of team groups
        /// to find other divisions with at least one team in that group.
        /// </summary>
        /// <param name="team">The division being examined</param>
        /// <param name="group">The group the division belongs to</param>
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
                {
                    // We've already visited that group
                    continue;
                }

                visitedGroups.Add(teamGroup);

                groupDivisions = (from t in entities.Teams
                         join c in entities.Conferences on t.ConferenceID equals c.ID
                         join d in entities.Divisions on c.DivisionID equals d.ID
                         where t.Group == teamGroup
                         && d.Group == null
                         select d).ToList();

                foreach (Division otherDivision in groupDivisions)
                {
                    // Its possible the division has already been examined
                    // so we don't want to call the recursive method if so.
                    entities.Entry(otherDivision).Reload();

                    if (otherDivision.Group == null)
                    {
                        ConnectDivisions(otherDivision, group);
                    }
                }
            }
        }
    }
}
