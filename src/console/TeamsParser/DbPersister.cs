using DatabaseLayer;
using Microsoft.EntityFrameworkCore;

namespace TeamsParser;

class DbPersister
{
    private readonly int _year;

    public DbPersister(int year)
    {
        _year = year;
    }

    public void Persist(IReadOnlyList<TeamEntry> entries)
    {
        using var db = new CollegeFootballEntities();

        // Load existing data into dictionaries for fast lookup (case-insensitive).
        // GroupBy + First handles any pre-existing case-insensitive duplicates in the DB.
        var divisions   = db.Divisions.AsEnumerable().GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var conferences = db.Conferences.AsEnumerable().GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var teams       = db.Teams.AsEnumerable().GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var existingConfAffils = db.ConferenceAffiliations
            .Where(ca => ca.Year == _year)
            .ToDictionary(ca => ca.ConferenceID);

        var existingTeamAffils = db.TeamAffiliations
            .Where(ta => ta.Year == _year)
            .ToDictionary(ta => ta.TeamID);

        int newDivisions = 0, newConferences = 0, newTeams = 0, newAffils = 0;

        foreach (var entry in entries)
        {
            // 1. Division
            if (!divisions.TryGetValue(entry.Division, out var division))
            {
                division = new Division { Name = entry.Division };
                db.Divisions.Add(division);
                db.SaveChanges();
                divisions[division.Name] = division;
                newDivisions++;
            }

            // 2. Conference
            if (!conferences.TryGetValue(entry.Conference, out var conference))
            {
                conference = new Conference { Name = entry.Conference, DivisionID = division.ID };
                db.Conferences.Add(conference);
                db.SaveChanges();
                conferences[conference.Name] = conference;
                newConferences++;
            }

            // 3. ConferenceAffiliation for this year
            if (!existingConfAffils.ContainsKey(conference.ID))
            {
                db.ConferenceAffiliations.Add(new ConferenceAffiliation
                {
                    ConferenceID = conference.ID,
                    DivisionID   = division.ID,
                    Year         = _year,
                });
                existingConfAffils[conference.ID] = null!;
                newAffils++;
            }

            // 4. Team
            if (!teams.TryGetValue(entry.Team, out var team))
            {
                team = new Team { Name = entry.Team, ConferenceID = conference.ID };
                db.Teams.Add(team);
                db.SaveChanges();
                teams[team.Name] = team;
                newTeams++;
            }

            // 5. TeamAffiliation for this year
            if (!existingTeamAffils.ContainsKey(team.ID))
            {
                db.TeamAffiliations.Add(new TeamAffiliation
                {
                    TeamID       = team.ID,
                    ConferenceID = conference.ID,
                    Year         = _year,
                });
                existingTeamAffils[team.ID] = null!;
                newAffils++;
            }
        }

        db.SaveChanges();

        Console.WriteLine($"DB persist ({_year}): +{newDivisions} divisions, +{newConferences} conferences, +{newTeams} teams, +{newAffils} affiliations");
    }
}
