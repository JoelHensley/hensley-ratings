using HensleyRatings.Api;
using Xunit;

namespace HensleyRatings.Api.Tests;

public class PredictionServiceTests
{
    // Helper: 10-game totals so per-game averages are whole numbers
    private static (int predicted_home, int predicted_away) Predict(
        double homeRating, double awayRating,
        int? homePtsScored, int? homePtsAllowed,
        int? awayPtsScored, int? awayPtsAllowed,
        bool isNeutralSite = false,
        int homeGp = 10, int awayGp = 10)
    {
        var result = PredictionService.Predict(
            homeRating, awayRating,
            homePtsScored, homePtsAllowed, homeGp,
            awayPtsScored, awayPtsAllowed, awayGp,
            isNeutralSite);
        Assert.NotNull(result);
        return result!.Value;
    }

    [Fact]
    public void HigherRatedHomeTeamWins()
    {
        var (home, away) = Predict(
            homeRating: 5.0, awayRating: 2.0,
            homePtsScored: 280, homePtsAllowed: 140,
            awayPtsScored: 200, awayPtsAllowed: 210);

        Assert.True(home > away, $"Expected home ({home}) > away ({away})");
    }

    [Fact]
    public void HigherRatedAwayTeamWins()
    {
        var (home, away) = Predict(
            homeRating: 1.0, awayRating: 6.0,
            homePtsScored: 140, homePtsAllowed: 280,
            awayPtsScored: 350, awayPtsAllowed: 100);

        Assert.True(away > home, $"Expected away ({away}) > home ({home})");
    }

    [Fact]
    public void NeutralSiteRemovesHomeFieldAdvantage()
    {
        // Home team marginally better, but not by more than HFA
        var (homeNeutral, awayNeutral) = Predict(
            homeRating: 3.5, awayRating: 3.0,
            homePtsScored: 280, homePtsAllowed: 200,
            awayPtsScored: 280, awayPtsAllowed: 200,
            isNeutralSite: true);

        var (homeHome, awayHome) = Predict(
            homeRating: 3.5, awayRating: 3.0,
            homePtsScored: 280, homePtsAllowed: 200,
            awayPtsScored: 280, awayPtsAllowed: 200,
            isNeutralSite: false);

        Assert.True(homeHome - awayHome >= homeNeutral - awayNeutral);
    }

    [Fact]
    public void ZeroGameCountReturnsNullPrediction()
    {
        var result = PredictionService.Predict(
            homeRating: 4.0, awayRating: 2.0,
            homePtsScored: null, homePtsAllowed: null, homeGameCount: 0,
            awayPtsScored: null, awayPtsAllowed: null, awayGameCount: 0,
            isNeutralSite: false);

        Assert.Null(result);
    }

    [Fact]
    public void EqualRatingsNeutralSiteProducesCloseGame()
    {
        var (home, away) = Predict(
            homeRating: 3.0, awayRating: 3.0,
            homePtsScored: 280, homePtsAllowed: 280,
            awayPtsScored: 280, awayPtsAllowed: 280,
            isNeutralSite: true);

        // LOV = 0; scores are always separated by exactly 1 (never a tie)
        Assert.Equal(1, Math.Abs(home - away));
    }

    [Fact]
    public void PredictedLosingScoreNeverNegative()
    {
        var (home, away) = Predict(
            homeRating: 10.0, awayRating: -5.0,
            homePtsScored: 400, homePtsAllowed: 50,
            awayPtsScored: 50, awayPtsAllowed: 400);

        Assert.True(away >= 0, $"Losing score ({away}) should not be negative");
    }

    [Fact]
    public void BlendedScoreUsesPointsAllowed()
    {
        // Two teams with same pts scored but different pts allowed should yield different predictions
        var (home1, away1) = Predict(
            homeRating: 5.0, awayRating: 2.0,
            homePtsScored: 280, homePtsAllowed: 100,   // tight defense
            awayPtsScored: 200, awayPtsAllowed: 300);  // leaky defense

        var (home2, away2) = Predict(
            homeRating: 5.0, awayRating: 2.0,
            homePtsScored: 280, homePtsAllowed: 100,
            awayPtsScored: 200, awayPtsAllowed: 100);  // tight defense

        // Leaky opponent defense means higher predicted score for home winner
        Assert.True(home1 > home2, $"Expected higher winning score ({home1}) when opponent defense is leaky vs ({home2})");
    }
}
