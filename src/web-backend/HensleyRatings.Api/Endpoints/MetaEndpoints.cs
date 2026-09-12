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

        app.MapGet("/api/meta/weeks", async (int year, CollegeFootballEntities db) =>
        {
            var weeks = await db.WeekSettings
                .Where(ws => ws.Year == year)
                .OrderBy(ws => ws.Week)
                .ToListAsync();

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
                .OrderBy(d => d.Name)
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

            var conferences = await query
                .Select(ca => new ConferenceResponse(ca.ConferenceID, ca.Conference.Name, ca.DivisionID))
                .Distinct()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Results.Ok(conferences);
        });
    }
}
