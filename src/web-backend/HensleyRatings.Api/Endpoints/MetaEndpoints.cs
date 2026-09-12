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
            IQueryable<WeekSettings> query = db.WeekSettings.Where(ws => ws.Year == year);
            if (allWeeks != true)
                query = query.Where(ws => db.TeamResults.Any(tr => tr.Year == year && tr.Week == ws.Week));
            var weeks = await query.OrderBy(ws => ws.Week).ToListAsync();

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
