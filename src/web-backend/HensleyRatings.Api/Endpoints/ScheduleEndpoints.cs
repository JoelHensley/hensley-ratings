using DatabaseLayer;
using HensleyRatings.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HensleyRatings.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this WebApplication app)
    {
        app.MapGet("/api/schedule", async (
            int year,
            int week,
            int? divisionId,
            int? conferenceId,
            CollegeFootballEntities db) =>
        {
            // Get cutoff date for this week
            var weekSetting = await db.WeekSettings
                .FirstOrDefaultAsync(ws => ws.Year == year && ws.Week == week);

            // Get previous week cutoff (for date range start)
            var prevWeekSetting = week > 1
                ? await db.WeekSettings.FirstOrDefaultAsync(ws => ws.Year == year && ws.Week == week - 1)
                : null;

            // Parse cutoff dates
            DateTime? cutoffEnd = weekSetting != null ? DateTime.Parse(weekSetting.CutoffDate) : null;
            DateTime? cutoffStart = prevWeekSetting != null ? DateTime.Parse(prevWeekSetting.CutoffDate) : null;

            var gamesQuery = db.Games.Where(g => g.Year == year);

            if (cutoffEnd.HasValue)
                gamesQuery = gamesQuery.Where(g => g.Date <= cutoffEnd.Value);
            if (cutoffStart.HasValue)
                gamesQuery = gamesQuery.Where(g => g.Date > cutoffStart.Value);

            var games = await gamesQuery
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .OrderBy(g => g.Date)
                .ToListAsync();

            if (!games.Any())
                return Results.Ok(Array.Empty<ScheduleGameResponse>());

            // Filter by division / conference if needed
            if (divisionId.HasValue || conferenceId.HasValue)
            {
                var confIdsInDiv = divisionId.HasValue
                    ? (await db.ConferenceAffiliations
                        .Where(ca => ca.Year == year && ca.DivisionID == divisionId.Value)
                        .Select(ca => ca.ConferenceID).ToListAsync())
                    : null;

                var teamIdsQuery = db.TeamAffiliations.Where(ta => ta.Year == year);
                if (confIdsInDiv != null)
                    teamIdsQuery = teamIdsQuery.Where(ta => confIdsInDiv.Contains(ta.ConferenceID));
                if (conferenceId.HasValue)
                    teamIdsQuery = teamIdsQuery.Where(ta => ta.ConferenceID == conferenceId.Value);

                var teamIds = await teamIdsQuery.Select(ta => ta.TeamID).ToListAsync();
                games = games.Where(g =>
                    teamIds.Contains(g.HomeTeamID) || teamIds.Contains(g.AwayTeamID)).ToList();
            }

            // Load ratings for both teams to power predictions + stats
            var allTeamIds = games.SelectMany(g => new[] { g.HomeTeamID, g.AwayTeamID }).Distinct().ToList();
            var ratings = await db.TeamResults
                .Where(tr => tr.Year == year && tr.Week == week && allTeamIds.Contains(tr.TeamID))
                .ToListAsync();

            var allResults = await db.TeamResults
                .Where(tr => tr.Year == year && tr.Week == week)
                .OrderByDescending(tr => tr.HensleyRating)
                .ToListAsync();

            var overallRank = allResults
                .Select((r, i) => (r.TeamID, Rank: i + 1))
                .ToDictionary(x => x.TeamID, x => x.Rank);

            var ratingsByTeam = ratings.ToDictionary(r => r.TeamID);

            // Get conference names via affiliations
            var affsByTeam = await db.TeamAffiliations
                .Where(ta => ta.Year == year && allTeamIds.Contains(ta.TeamID))
                .Include(ta => ta.Conference)
                .ToDictionaryAsync(ta => ta.TeamID);

            var response = games.Select(g =>
            {
                var isComplete = g.HomeScore > 0 || g.AwayScore > 0;
                ratingsByTeam.TryGetValue(g.HomeTeamID, out var homeRating);
                ratingsByTeam.TryGetValue(g.AwayTeamID, out var awayRating);
                overallRank.TryGetValue(g.HomeTeamID, out var homeRank);
                overallRank.TryGetValue(g.AwayTeamID, out var awayRank);

                affsByTeam.TryGetValue(g.HomeTeamID, out var homeAff);
                affsByTeam.TryGetValue(g.AwayTeamID, out var awayAff);

                int? predHome = null, predAway = null;
                if (!isComplete && homeRating != null && awayRating != null)
                {
                    (predHome, predAway) = PredictionService.Predict(
                        homeRating.HensleyRating,
                        awayRating.HensleyRating,
                        homeRating.PointsScored,
                        homeRating.PointsAllowed,
                        awayRating.PointsScored,
                        awayRating.PointsAllowed,
                        g.IsNeutralSite
                    );
                }

                return new ScheduleGameResponse(
                    g.ID,
                    g.Date.ToString("yyyy-MM-dd"),
                    g.HomeTeamID,
                    g.HomeTeam?.Name ?? "",
                    homeAff?.Conference?.Name ?? "",
                    isComplete ? g.HomeScore : null,
                    g.AwayTeamID,
                    g.AwayTeam?.Name ?? "",
                    awayAff?.Conference?.Name ?? "",
                    isComplete ? g.AwayScore : null,
                    g.IsNeutralSite,
                    isComplete,
                    predHome,
                    predAway,
                    homeRating?.HensleyRating,
                    awayRating?.HensleyRating,
                    homeRank == 0 ? null : homeRank,
                    awayRank == 0 ? null : awayRank,
                    homeRating?.PointsScored,
                    awayRating?.PointsScored
                );
            }).ToList();

            return Results.Ok(response);
        });
    }
}
