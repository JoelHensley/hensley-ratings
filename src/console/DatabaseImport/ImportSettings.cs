using System;

namespace DataImport
{
    class ImportSettings
    {
        public bool ImportTeams = true;
        public bool ImportGames = true;
        public bool ImportWeekSettings = true;
        public bool CreateGroups = true;
        public int Year;
        public int Week;
        public string TeamsFileName;
        public string GamesFileName = "converted-games.csv";
        public string WeekSettingsFileName;

        public ImportSettings()
        {
            TeamsFileName = Environment.GetEnvironmentVariable("TEAMS_DATA_FILE") ?? "teams.csv";
            WeekSettingsFileName = Environment.GetEnvironmentVariable("WEEK_SETTINGS_FILE") ?? "week-settings.csv";

            if (!int.TryParse(Environment.GetEnvironmentVariable("SEASON_YEAR"), out Year))
                Year = DateTime.Now.Year;

            if (!int.TryParse(Environment.GetEnvironmentVariable("WEEK"), out Week))
                Week = 1;
        }
    }
}
