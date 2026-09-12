using System;

namespace DataImport
{
    class ImportSettings
    {
        public bool ImportTeams = true;
        public bool ImportGames = true;
        public bool ImportWeekSettings = true;
        public bool CreateGroups = true;
        public bool WipeDbOnStart;
        public int Year;
        public string TeamsFileName;
        public string GamesFileName = "converted-games.csv";
        public string WeekSettingsFileName;

        public ImportSettings()
        {
            TeamsFileName = Environment.GetEnvironmentVariable("TEAMS_DATA_FILE") ?? "teams.csv";
            WeekSettingsFileName = Environment.GetEnvironmentVariable("WEEK_SETTINGS_FILE") ?? "week-settings.csv";

            if (!int.TryParse(Environment.GetEnvironmentVariable("SEASON_YEAR"), out Year))
                Year = DateTime.Now.Year;

            WipeDbOnStart = !(Environment.GetEnvironmentVariable("WIPE_DB_ON_START") ?? "true")
                              .Equals("false", StringComparison.OrdinalIgnoreCase);
        }
    }
}
