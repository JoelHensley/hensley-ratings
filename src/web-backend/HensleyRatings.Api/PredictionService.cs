namespace HensleyRatings.Api;

/// <summary>
/// Ports the PHP game-prediction.php formula exactly.
/// LOV = hensleyRating_higher - hensleyRating_lower (+ 1.0 for home field)
/// winningScore = avgPointsScored_winner
/// losingScore  = winningScore - LOV * sqrt(winningScore)  (floor 0)
/// </summary>
public static class PredictionService
{
    private const double HomeFieldAdvantage = 1.0;
    private const double FallbackAvgPoints = 28.0;

    public static (int predicted_home, int predicted_away) Predict(
        double homeRating,
        double awayRating,
        int? homePtsScored,
        int? homePtsAllowed,
        int? awayPtsScored,
        int? awayPtsAllowed,
        bool isNeutralSite)
    {
        double homeAdj = isNeutralSite ? 0 : HomeFieldAdvantage;
        double effectiveHome = homeRating + homeAdj;

        bool homeWins = effectiveHome >= awayRating;
        double winnerRating = homeWins ? effectiveHome : awayRating;
        double loserRating = homeWins ? awayRating : effectiveHome;
        double lov = winnerRating - loserRating;

        double winnerAvgPts = homeWins
            ? GetAvgPoints(homePtsScored)
            : GetAvgPoints(awayPtsScored);

        double losingScore = winnerAvgPts - lov * Math.Sqrt(Math.Max(winnerAvgPts, 1));
        losingScore = Math.Max(0, losingScore);

        int winScore = (int)Math.Round(winnerAvgPts);
        int loseScore = (int)Math.Round(losingScore);

        return homeWins ? (winScore, loseScore) : (loseScore, winScore);
    }

    private static double GetAvgPoints(int? ptsScored) =>
        ptsScored.HasValue && ptsScored.Value > 0 ? ptsScored.Value : FallbackAvgPoints;
}
