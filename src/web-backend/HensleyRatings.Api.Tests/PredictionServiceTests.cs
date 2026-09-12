using HensleyRatings.Api;
using Xunit;

namespace HensleyRatings.Api.Tests;

public class PredictionServiceTests
{
    [Fact]
    public void HigherRatedHomeTeamWins()
    {
        var (home, away) = PredictionService.Predict(
            homeRating: 5.0, awayRating: 2.0,
            homePtsScored: 280, homePtsAllowed: 140,
            awayPtsScored: 200, awayPtsAllowed: 210,
            isNeutralSite: false);

        Assert.True(home > away, $"Expected home ({home}) > away ({away})");
    }

    [Fact]
    public void HigherRatedAwayTeamWins()
    {
        var (home, away) = PredictionService.Predict(
            homeRating: 1.0, awayRating: 6.0,
            homePtsScored: 140, homePtsAllowed: 280,
            awayPtsScored: 350, awayPtsAllowed: 100,
            isNeutralSite: false);

        Assert.True(away > home, $"Expected away ({away}) > home ({home})");
    }

    [Fact]
    public void NeutralSiteRemovesHomeFieldAdvantage()
    {
        // Home team marginally better, but not by more than HFA
        var (homeNeutral, awayNeutral) = PredictionService.Predict(
            homeRating: 3.5, awayRating: 3.0,
            homePtsScored: 280, homePtsAllowed: 200,
            awayPtsScored: 280, awayPtsAllowed: 200,
            isNeutralSite: true);

        var (homeHome, awayHome) = PredictionService.Predict(
            homeRating: 3.5, awayRating: 3.0,
            homePtsScored: 280, homePtsAllowed: 200,
            awayPtsScored: 280, awayPtsAllowed: 200,
            isNeutralSite: false);

        // Home field game should result in bigger home margin
        Assert.True(homeHome - awayHome >= homeNeutral - awayNeutral);
    }

    [Fact]
    public void NullPointsUseFallback()
    {
        // Should not throw; falls back to FallbackAvgPoints = 28
        var (home, away) = PredictionService.Predict(
            homeRating: 4.0, awayRating: 2.0,
            homePtsScored: null, homePtsAllowed: null,
            awayPtsScored: null, awayPtsAllowed: null,
            isNeutralSite: false);

        Assert.True(home > 0);
        Assert.True(away >= 0);
    }

    [Fact]
    public void EqualRatingsNeutralSiteCloseGame()
    {
        var (home, away) = PredictionService.Predict(
            homeRating: 3.0, awayRating: 3.0,
            homePtsScored: 280, homePtsAllowed: 280,
            awayPtsScored: 280, awayPtsAllowed: 280,
            isNeutralSite: true);

        // LOV = 0, so losing score = winning score - 0 * sqrt(winning) = winning score → tie
        Assert.Equal(home, away);
    }

    [Fact]
    public void PredictedLosingScoreNeverNegative()
    {
        // Extreme rating difference
        var (home, away) = PredictionService.Predict(
            homeRating: 10.0, awayRating: -5.0,
            homePtsScored: 400, homePtsAllowed: 50,
            awayPtsScored: 50, awayPtsAllowed: 400,
            isNeutralSite: false);

        Assert.True(away >= 0, $"Losing score ({away}) should not be negative");
    }
}
