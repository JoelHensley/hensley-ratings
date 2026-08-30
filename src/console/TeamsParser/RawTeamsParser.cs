using System.Text.RegularExpressions;

namespace TeamsParser;

record TeamEntry(string Division, string Conference, string Team);

class RawTeamsParser
{
    // Matches conference lines: optional whitespace, one or more uppercase letters, period, spaces, name
    private static readonly Regex ConferenceLine = new(@"^\s+([A-Z]+)\.\s+(.+)$");

    // Matches sub-division lines: optional whitespace, Roman numeral, closing paren
    private static readonly Regex SubDivisionLine = new(@"^\s+(i{1,3}|iv|vi{0,3}|ix|v)\)\s+", RegexOptions.IgnoreCase);

    public List<TeamEntry> Parse(string filePath)
    {
        var entries = new List<TeamEntry>();

        string currentDivision  = "";
        string currentConference = "";

        foreach (var raw in File.ReadLines(filePath))
        {
            string trimmed = raw.TrimEnd();

            // Skip blank lines and known noise
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            if (trimmed.Equals("Back to top", StringComparison.OrdinalIgnoreCase)) continue;

            string stripped = trimmed.TrimStart();

            // Section header: no leading whitespace → division boundary
            if (stripped == trimmed)
            {
                // Strip trailing colon (e.g. "Other:")
                string header = stripped.TrimEnd(':');
                if (KnownMappings.DivisionAliases.TryGetValue(header, out string? div))
                {
                    currentDivision = div;
                    // "Other" section has no conference sub-headers — default conference to division name
                    currentConference = div == "Other" ? "Other" : "";
                }
                else if (!string.IsNullOrEmpty(header))
                    Console.Error.WriteLine($"[WARN] Unknown division header: \"{header}\"");
                continue;
            }

            // Conference line: "A.  Conference Name"
            var confMatch = ConferenceLine.Match(trimmed);
            if (confMatch.Success)
            {
                string raw_conf = confMatch.Groups[2].Value.Trim();
                currentConference = KnownMappings.ConferenceAliases.TryGetValue(raw_conf, out string? alias)
                    ? alias
                    : raw_conf;
                continue;
            }

            // Sub-division line: "i) East Division" — skip, keep current conference
            if (SubDivisionLine.IsMatch(trimmed)) continue;

            // Team line
            if (string.IsNullOrEmpty(currentDivision) || string.IsNullOrEmpty(currentConference))
            {
                Console.Error.WriteLine($"[WARN] Team line before division/conference set: \"{stripped}\"");
                continue;
            }

            // Strip trailing & (used in Other section for non-NCAA teams)
            string teamRaw = stripped.TrimEnd('&').Trim();
            if (string.IsNullOrEmpty(teamRaw)) continue;

            string team = KnownMappings.TeamAliases.TryGetValue(teamRaw, out string? teamAlias)
                ? teamAlias
                : teamRaw;

            entries.Add(new TeamEntry(currentDivision, currentConference, team));
        }

        return entries;
    }
}
