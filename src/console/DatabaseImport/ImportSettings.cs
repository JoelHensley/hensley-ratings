/* ImportSettings.cs
 * Joel Hensley
 * January 13, 2010
 * This class contains the settings for the data import.
 */
namespace DataImport
{
    class ImportSettings
    {
        public bool DeleteExistingData = true;
        public bool ImportTeams = true;
        public bool ImportGames = true;
        public bool CreateGroups = true;
        public string TeamsFileName = "teams.csv";
        public string GamesFileName = "converted-games.csv";
    }
}
