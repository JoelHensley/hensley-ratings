namespace TeamsParser;

class ParserSettings
{
    public string RawTeamsFileName;
    public string PreviousTeamsFile;
    public string OutputFileName;

    public ParserSettings()
    {
        RawTeamsFileName  = Resolve(Environment.GetEnvironmentVariable("RAW_TEAMS_FILE")           ?? "BuildFiles/raw-teams.txt");
        PreviousTeamsFile = Resolve(Environment.GetEnvironmentVariable("PREVIOUS_TEAMS_DATA_FILE") ?? "");
        OutputFileName    = Resolve(Environment.GetEnvironmentVariable("OUTPUT_TEAMS_FILE")        ?? "BuildFiles/teams.csv");
    }

    private static string Resolve(string path) =>
        path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
}
