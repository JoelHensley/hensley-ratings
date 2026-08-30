/* TeamGraph.cs
 * Joel Hensley
 * January 27, 2010
 * This class is used to identify and store the different team
 * groups in the database. A team is connected to another team
 * if there is a series of games that connects to the teams together.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DatabaseLayer;

namespace DataImport
{
    class TeamGraph
    {
        private CollegeFootballEntities entities = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_entities">The database object</param>
        public TeamGraph(CollegeFootballEntities _entities)
        {
            entities = _entities;
        }

        /// <summary>
        /// Loops through all the teams in the database until all
        /// teams are included in some group.
        /// </summary>
        public void CreateTeamGroups()
        {
            Team team;
            int group = 1;

            team = entities.Teams.FirstOrDefault(t => t.Group == null);
            while (team != null)
            {
                ConnectTeams(team, group);
                team = entities.Teams.FirstOrDefault(t => t.Group == null);
                group++;
            }
        }

        /// <summary>
        /// Recursively loops through a team's opponents until
        /// all teams in the group are found.
        /// </summary>
        /// <param name="team">The team being examined</param>
        /// <param name="group">The group the team belongs to</param>
        private void ConnectTeams(Team team, int group)
        {
            team.Group = group;
            entities.SaveChanges();

            foreach (Team opponent in team.Opponents.Where(t => t.Group == null))
            {
                ConnectTeams(opponent, group);
            }
        }
    }
}
