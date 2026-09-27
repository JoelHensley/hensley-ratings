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

            // All results for the year across all weeks — used for per-week ranking
            var allYearlyResults = await db.TeamResults
                .Where(tr => tr.Year == year)
                .ToListAsync();

            var allResults = allYearlyResults
                .Where(tr => tr.Week == week)
                .OrderByDescending(tr => tr.HensleyRating)
                .ToList();

            var overallRank = allResults
                .Select((r, i) => (r.TeamID, Rank: i + 1))
                .ToDictionary(x => x.TeamID, x => x.Rank);
            overallRank.TryGetValue(id, out var teamRank);

            // Per-week rating rank and schedule strength rank (for game log deltas)
            var ratingRankByWeek = allYearlyResults
                .GroupBy(tr => tr.Week)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.HensleyRating)
                          .Select((r, i) => (r.TeamID, Rank: i + 1))
                          .ToDictionary(x => x.TeamID, x => x.Rank));

            var schedRankByWeek = allYearlyResults
                .GroupBy(tr => tr.Week)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.ScheduleStrength)
                          .Select((r, i) => (r.TeamID, Rank: i + 1))
                          .ToDictionary(x => x.TeamID, x => x.Rank));

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
                int? teamRankDelta = null;
                double? schedStrength = null;
                int? schedRank = null;
                int? schedRankDelta = null;
                if (gameWeek != null && weeklyRatingByWeek.TryGetValue(gameWeek.Week, out var wr))
                {
                    teamRatingAtWeek = wr.HensleyRating;
                    schedStrength = wr.ScheduleStrength;

                    // Per-week rating rank and rank delta
                    if (ratingRankByWeek.TryGetValue(gameWeek.Week, out var ratingRanks)
                        && ratingRanks.TryGetValue(id, out var rr))
                    {
                        teamRankAtWeek = rr;
                        if (ratingRankByWeek.TryGetValue(gameWeek.Week - 1, out var prevRatingRanks)
                            && prevRatingRanks.TryGetValue(id, out var prevRr))
                            teamRankDelta = prevRr - rr; // positive = moved up
                    }

                    // Per-week schedule strength rank and rank delta
                    if (schedRankByWeek.TryGetValue(gameWeek.Week, out var schedRanks)
                        && schedRanks.TryGetValue(id, out var sr))
                    {
                        schedRank = sr;
                        if (schedRankByWeek.TryGetValue(gameWeek.Week - 1, out var prevSchedRanks)
                            && prevSchedRanks.TryGetValue(id, out var prevSr))
                            schedRankDelta = prevSr - sr; // positive = moved up
                    }
                }

                oppRankByTeam.TryGetValue(opponentId, out var oppRank);

                int? predTeamScore = null, predOppScore = null;
                if (!isComplete && ratingsByTeamId.TryGetValue(opponentId, out var oppRating))
                {
                    double homeRating = isHome ? latestResult.HensleyRating : oppRating.HensleyRating;
                    double awayRating = isHome ? oppRating.HensleyRating : latestResult.HensleyRating;
                    int? homePtsScored  = isHome ? latestResult.PointsScored  : oppRating.PointsScored;
                    int? homePtsAllowed = isHome ? latestResult.PointsAllowed : oppRating.PointsAllowed;
                    int  homeGp         = isHome ? latestResult.Wins + latestResult.Losses : oppRating.Wins + oppRating.Losses;
                    int? awayPtsScored  = isHome ? oppRating.PointsScored  : latestResult.PointsScored;
                    int? awayPtsAllowed = isHome ? oppRating.PointsAllowed : latestResult.PointsAllowed;
                    int  awayGp         = isHome ? oppRating.Wins + oppRating.Losses : latestResult.Wins + latestResult.Losses;

                    var pred = PredictionService.Predict(homeRating, awayRating, homePtsScored, homePtsAllowed, homeGp, awayPtsScored, awayPtsAllowed, awayGp, g.IsNeutralSite);
                    if (pred.HasValue)
                    {
                        predTeamScore = isHome ? pred.Value.predicted_home : pred.Value.predicted_away;
                        predOppScore  = isHome ? pred.Value.predicted_away : pred.Value.predicted_home;
                    }
                }

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
                    teamRankAtWeek,
                    teamRankDelta,
                    schedStrength,
                    schedRank,
                    schedRankDelta,
                    gameWeek?.Week,
                    predTeamScore,
                    predOppScore
                );
            }).ToList();

            // Weeks that have ratings but no game — bye weeks
            var gameWeekNumbers = gameLog.Where(g => g.WeekNumber.HasValue).Select(g => g.WeekNumber!.Value).ToHashSet();
            var byeWeeks = weeklyRatingByWeek.Keys
                .Where(w => !gameWeekNumbers.Contains(w))
                .OrderBy(w => w)
                .Select(w =>
                {
                    var wr = weeklyRatingByWeek[w];
                    int ratingRank = 0, schedRank2 = 0;
                    if (ratingRankByWeek.TryGetValue(w, out var rr)) rr.TryGetValue(id, out ratingRank);
                    if (schedRankByWeek.TryGetValue(w, out var sr)) sr.TryGetValue(id, out schedRank2);

                    int? ratingDelta = null, schedDelta = null;
                    if (ratingRankByWeek.TryGetValue(w - 1, out var prevRr) && prevRr.TryGetValue(id, out var prevRating))
                        ratingDelta = prevRating - ratingRank;
                    if (schedRankByWeek.TryGetValue(w - 1, out var prevSr) && prevSr.TryGetValue(id, out var prevSched))
                        schedDelta = prevSched - schedRank2;

                    return new ByeWeekResponse(w, wr.Wins, wr.Losses, wr.HensleyRating, ratingRank, ratingDelta, wr.ScheduleStrength, schedRank2, schedDelta);
                })
                .ToList();

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
                gameLog,
                byeWeeks
            );

            return Results.Ok(response);
        });
    }
}
