using DatabaseLayer;
using HensleyRatings.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HensleyRatings.Api.Endpoints;

public static class MetaEndpoints
{
    public static void MapMetaEndpoints(this WebApplication app)
    {
        app.MapGet("/api/meta/years", async (CollegeFootballEntities db) =>
        {
            var years = await db.TeamResults
                .Select(tr => tr.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            return Results.Ok(new MetaYearsResponse(years));
        });

        app.MapGet("/api/meta/weeks", async (int year, bool? allWeeks, CollegeFootballEntities db) =>
        {
            var allWeekSettings = await db.WeekSettings
                .Where(ws => ws.Year == year)
                .OrderBy(ws => ws.Week)
                .ToListAsync();

            List<WeekSettings> weeks;
            if (allWeeks == true)
            {
                weeks = allWeekSettings;
            }
            else
            {
                weeks = new List<WeekSettings>();
                for (int i = 0; i < allWeekSettings.Count; i++)
                {
                    var ws = allWeekSettings[i];
                    var cutoff = DateTime.Parse(ws.CutoffDate);
                    var prevCutoff = i > 0 ? DateTime.Parse(allWeekSettings[i - 1].CutoffDate) : DateTime.MinValue;
                    bool hasGames = await db.Games.AnyAsync(g =>
                        g.Year == year &&
                        (g.HomeScore > 0 || g.AwayScore > 0) &&
                        g.Date <= cutoff &&
                        g.Date > prevCutoff);
                    if (hasGames)
                        weeks.Add(ws);
                }
            }

            var options = weeks.Select((ws, i) => new WeekOption(
                ws.Week,
                ws.CutoffDate,
                i > 0
            ));
            return Results.Ok(new MetaWeeksResponse(options));
        });

        app.MapGet("/api/meta/divisions", async (CollegeFootballEntities db) =>
        {
            var divisions = await db.Divisions
                .Where(d => d.Name != "Other")
                .OrderBy(d => d.ID)
                .Select(d => new DivisionResponse(d.ID, d.Name))
                .ToListAsync();
            return Results.Ok(divisions);
        });

        app.MapGet("/api/meta/conferences", async (int year, int? divisionId, CollegeFootballEntities db) =>
        {
            var query = db.ConferenceAffiliations
                .Where(ca => ca.Year == year);

            if (divisionId.HasValue)
                query = query.Where(ca => ca.DivisionID == divisionId.Value);

            var raw = await query
                .Select(ca => new ConferenceResponse(ca.ConferenceID, ca.Conference.Name, ca.DivisionID))
                .ToListAsync();

            var conferences = raw.DistinctBy(c => c.ConferenceId).OrderBy(c => c.Name).ToList();
            return Results.Ok(conferences);
        });
    }
}
