/* ImportSettings.cs
 * Joel Hensley
 * January 13, 2010
 * This class contains the settings for the data import.
 */
using System;

namespace DataImport
{
    class ImportSettings
    {
        public bool DeleteExistingData = true;
        public bool ImportTeams = true;
        public bool ImportGames = true;
        public bool CreateGroups = true;
        public string TeamsFileName;
        public string GamesFileName = "converted-games.csv";

        public ImportSettings()
        {
            TeamsFileName = Environment.GetEnvironmentVariable("TEAMS_DATA_FILE") ?? "teams.csv";
        }
    }
}
