using DatabaseLayer;
using HensleyRatings.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HensleyRatings.Api.Endpoints;

public static class TeamsEndpoints
{
    public static void MapTeamsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/teams/{id:int}", async (int id, int year, CollegeFootballEntities db) =>
        {
            var team = await db.Teams.FindAsync(id);
            if (team == null) return Results.NotFound();

            // Get latest week for this team/year
            var latestResult = await db.TeamResults
                .Where(tr => tr.TeamID == id && tr.Year == year)
                .OrderByDescending(tr => tr.Week)
                .FirstOrDefaultAsync();

            if (latestResult == null) return Results.NotFound();

            var week = latestResult.Week;

            // Overall rank
            var allResults = await db.TeamResults
                .Where(tr => tr.Year == year && tr.Week == week)
                .OrderByDescending(tr => tr.HensleyRating)
                .ToListAsync();

            var overallRank = allResults
                .Select((r, i) => (r.TeamID, Rank: i + 1))
                .ToDictionary(x => x.TeamID, x => x.Rank);
            overallRank.TryGetValue(id, out var teamRank);

            // Affiliation
            var aff = await db.TeamAffiliations
                .Where(ta => ta.TeamID == id && ta.Year == year)
                .Include(ta => ta.Conference)
                .FirstOrDefaultAsync();
            var confAff = aff != null
                ? await db.ConferenceAffiliations
                    .Include(ca => ca.Division)
                    .FirstOrDefaultAsync(ca => ca.ConferenceID == aff.ConferenceID && ca.Year == year)
                : null;

            // Games for this year
            var games = await db.Games
                .Where(g => (g.HomeTeamID == id || g.AwayTeamID == id) && g.Year == year)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .OrderBy(g => g.Date)
                .ToListAsync();

            // All opponent IDs
            var opponentIds = games.Select(g => g.HomeTeamID == id ? g.AwayTeamID : g.HomeTeamID).Distinct().ToList();

            // Ratings by team for ranking opponents — use week of game or latest available
            var weekSettings = await db.WeekSettings
                .Where(ws => ws.Year == year)
                .OrderBy(ws => ws.Week)
                .ToListAsync();

            var allOpponentResults = await db.TeamResults
                .Where(tr => tr.Year == year && tr.Week == week && opponentIds.Contains(tr.TeamID))
                .ToListAsync();

            var oppRankByTeam = allOpponentResults
                .ToDictionary(r => r.TeamID, r => overallRank.TryGetValue(r.TeamID, out var rank) ? rank : (int?)null);
            var ratingsByTeamId = allOpponentResults.ToDictionary(r => r.TeamID);

            // Weekly snapshots of the team's own ratings for per-game tracking
            var teamWeeklyResults = await db.TeamResults
                .Where(tr => tr.TeamID == id && tr.Year == year)
                .OrderBy(tr => tr.Week)
                .ToListAsync();

            var weeklyRatingByWeek = teamWeeklyResults.ToDictionary(tr => tr.Week);

            // Build game log with running record
            int wins = 0, losses = 0;
            var gameLog = games.Select(g =>
            {
                var isHome = g.HomeTeamID == id;
                var opponentId = isHome ? g.AwayTeamID : g.HomeTeamID;
                var opponentName = isHome ? g.AwayTeam?.Name : g.HomeTeam?.Name;
                var teamScore = isHome ? g.HomeScore : g.AwayScore;
                var oppScore = isHome ? g.AwayScore : g.HomeScore;
                var isComplete = teamScore > 0 || oppScore > 0;
                bool? isWin = isComplete ? teamScore > oppScore : null;

                if (isWin == true) wins++;
                else if (isWin == false) losses++;

                // Find the week this game falls in
                var gameWeek = weekSettings
                    .Where(ws => g.Date <= DateTime.Parse(ws.CutoffDate))
                    .OrderBy(ws => ws.Week)
                    .FirstOrDefault();

                double? teamRatingAtWeek = null;
                int? teamRankAtWeek = null;
                if (gameWeek != null && weeklyRatingByWeek.TryGetValue(gameWeek.Week, out var wr))
                {
                    teamRatingAtWeek = wr.HensleyRating;
                    // Compute rank for that week
                    teamRankAtWeek = allResults
                        .OrderByDescending(r => r.HensleyRating)
                        .TakeWhile(r => r.TeamID != id)
                        .Count() + 1;
                }

                oppRankByTeam.TryGetValue(opponentId, out var oppRank);

                return new TeamGameResponse(
                    g.ID,
                    g.Date.ToString("yyyy-MM-dd"),
                    isHome,
                    g.IsNeutralSite,
                    opponentId,
                    opponentName ?? "Unknown",
                    oppRank,
                    isComplete ? teamScore : null,
                    isComplete ? oppScore : null,
                    isWin,
                    wins,
                    losses,
                    teamRatingAtWeek,
                    teamRankAtWeek
                );
            }).ToList();

            var response = new TeamDetailResponse(
                id,
                team.Name,
                aff?.Conference?.Name ?? "",
                aff?.ConferenceID ?? 0,
                confAff?.Division?.Name ?? "",
                confAff?.DivisionID ?? 0,
                latestResult.Wins,
                latestResult.Losses,
                latestResult.HensleyRating,
                teamRank,
                latestResult.ScheduleStrength,
                latestResult.PointsScored,
                latestResult.PointsAllowed,
                gameLog
            );

            return Results.Ok(response);
        });
    }
}
