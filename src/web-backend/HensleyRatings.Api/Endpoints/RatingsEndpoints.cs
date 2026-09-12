using DatabaseLayer;
using HensleyRatings.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HensleyRatings.Api.Endpoints;

public static class RatingsEndpoints
{
    public static void MapRatingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/ratings", async (
            int year,
            int week,
            int? divisionId,
            int? conferenceId,
            CollegeFootballEntities db) =>
        {
            // Resolve which teams are in scope via affiliation for this year
            var teamIdsQuery = db.TeamAffiliations
                .Where(ta => ta.Year == year);

            if (divisionId.HasValue)
            {
                var confIds = db.ConferenceAffiliations
                    .Where(ca => ca.Year == year && ca.DivisionID == divisionId.Value)
                    .Select(ca => ca.ConferenceID);
                teamIdsQuery = teamIdsQuery.Where(ta => confIds.Contains(ta.ConferenceID));
            }

            if (conferenceId.HasValue)
                teamIdsQuery = teamIdsQuery.Where(ta => ta.ConferenceID == conferenceId.Value);

            var teamIds = await teamIdsQuery.Select(ta => ta.TeamID).ToListAsync();

            // Current week results
            var results = await db.TeamResults
                .Where(tr => tr.Year == year && tr.Week == week && teamIds.Contains(tr.TeamID))
                .Include(tr => tr.Team)
                .ToListAsync();

            if (!results.Any())
                return Results.Ok(Array.Empty<RatedTeam>());

            // Compute ranks via in-memory sort
            var allForRank = await db.TeamResults
                .Where(tr => tr.Year == year && tr.Week == week)
                .ToListAsync();

            var allAffsForYear = await db.TeamAffiliations
                .Where(ta => ta.Year == year)
                .Include(ta => ta.Conference)
                .ToListAsync();

            var confAffByConf = await db.ConferenceAffiliations
                .Where(ca => ca.Year == year)
                .ToDictionaryAsync(ca => ca.ConferenceID);

            var divisionsByConf = new Dictionary<int, string>();
            foreach (var ca in confAffByConf.Values)
                divisionsByConf[ca.ConferenceID] = (await db.Divisions.FindAsync(ca.DivisionID))?.Name ?? "";

            // Overall rank
            var sortedAll = allForRank.OrderByDescending(r => r.HensleyRating).ToList();
            var overallRank = sortedAll.Select((r, i) => (r.TeamID, Rank: i + 1))
                .ToDictionary(x => x.TeamID, x => x.Rank);

            // Previous week for change calculation
            var prevWeek = week - 1;
            Dictionary<int, int>? prevOverallRank = null;
            if (prevWeek > 0)
            {
                var prevResults = await db.TeamResults
                    .Where(tr => tr.Year == year && tr.Week == prevWeek)
                    .OrderByDescending(tr => tr.HensleyRating)
                    .ToListAsync();
                prevOverallRank = prevResults
                    .Select((r, i) => (r.TeamID, Rank: i + 1))
                    .ToDictionary(x => x.TeamID, x => x.Rank);
            }

            // Division rank: group by DivisionID via conference affiliation
            var byDiv = allForRank
                .GroupBy(r =>
                {
                    var aff = allAffsForYear.FirstOrDefault(ta => ta.TeamID == r.TeamID);
                    if (aff == null) return 0;
                    return confAffByConf.TryGetValue(aff.ConferenceID, out var ca) ? ca.DivisionID : 0;
                })
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.HensleyRating)
                    .Select((r, i) => (r.TeamID, Rank: i + 1))
                    .ToDictionary(x => x.TeamID, x => x.Rank));

            // Conference rank
            var byConf = allForRank
                .GroupBy(r =>
                    allAffsForYear.FirstOrDefault(ta => ta.TeamID == r.TeamID)?.ConferenceID ?? 0)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.HensleyRating)
                    .Select((r, i) => (r.TeamID, Rank: i + 1))
                    .ToDictionary(x => x.TeamID, x => x.Rank));

            var response = results.Select(r =>
            {
                var aff = allAffsForYear.FirstOrDefault(ta => ta.TeamID == r.TeamID);
                var confName = aff?.Conference?.Name ?? "";
                var divId = aff != null && confAffByConf.TryGetValue(aff.ConferenceID, out var ca2)
                    ? ca2.DivisionID : 0;
                var divName = divId > 0
                    ? (db.Divisions.Find(divId)?.Name ?? "") : "";

                overallRank.TryGetValue(r.TeamID, out var overall);
                byDiv.TryGetValue(divId, out var divRanks);
                var divRank = 0;
                divRanks?.TryGetValue(r.TeamID, out divRank);
                byConf.TryGetValue(aff?.ConferenceID ?? 0, out var confRanks);
                var confRank = 0;
                confRanks?.TryGetValue(r.TeamID, out confRank);

                int? change = null;
                if (prevOverallRank != null && prevOverallRank.TryGetValue(r.TeamID, out var prevRank))
                    change = prevRank - overall; // positive = moved up

                return new RatedTeam(
                    r.TeamID,
                    r.Team?.Name ?? "",
                    confName,
                    divName,
                    r.Wins,
                    r.Losses,
                    r.HensleyRating,
                    r.ScheduleStrength,
                    overall,
                    divRank == 0 ? overall : divRank,
                    confRank == 0 ? overall : confRank,
                    change,
                    r.PointsScored,
                    r.PointsAllowed
                );
            }).OrderBy(t => t.RankOverall).ToList();

            return Results.Ok(response);
        });
    }
}
