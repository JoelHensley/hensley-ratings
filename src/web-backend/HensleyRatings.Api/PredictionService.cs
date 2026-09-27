namespace HensleyRatings.Api;

/// <summary>
/// Ports the PHP game-prediction.php formula exactly.
///
/// baseWinningScore = (winnerPtsScored/gp + loserPtsAllowed/gp) / 2
/// baseLosingScore  = (loserPtsScored/gp  + winnerPtsAllowed/gp) / 2
///
/// Two formulas are run and their results averaged:
///   GetScoreFromWinningScore: losingScore = baseWin - LOV * sqrt(baseWin)
///   GetScoreFromLosingScore:  winningScore = (LOV² + 2*baseLose + LOV*sqrt(LOV²+4*baseLose)) / 2
/// </summary>
public static class PredictionService
{
    private const double HomeFieldAdvantage = 1.0;

    public static (int predicted_home, int predicted_away)? Predict(
        double homeRating,
        double awayRating,
        int? homePtsScored,
        int? homePtsAllowed,
        int homeGameCount,
        int? awayPtsScored,
        int? awayPtsAllowed,
        int awayGameCount,
        bool isNeutralSite)
    {
        if (homeGameCount == 0 || awayGameCount == 0)
            return null;

        double effectiveHome = homeRating + (isNeutralSite ? 0 : HomeFieldAdvantage);
        bool homeWins = effectiveHome >= awayRating;
        double lov = homeWins ? effectiveHome - awayRating : awayRating - effectiveHome;

        double winnerPtsScored  = homeWins ? (homePtsScored  ?? 0) : (awayPtsScored  ?? 0);
        double winnerPtsAllowed = homeWins ? (homePtsAllowed ?? 0) : (awayPtsAllowed ?? 0);
        double loserPtsScored   = homeWins ? (awayPtsScored  ?? 0) : (homePtsScored  ?? 0);
        double loserPtsAllowed  = homeWins ? (awayPtsAllowed ?? 0) : (homePtsAllowed ?? 0);
        int winnerGp = homeWins ? homeGameCount : awayGameCount;
        int loserGp  = homeWins ? awayGameCount : homeGameCount;

        double baseWin  = (winnerPtsScored / winnerGp + loserPtsAllowed / loserGp) / 2.0;
        double baseLose = (loserPtsScored  / loserGp  + winnerPtsAllowed / winnerGp) / 2.0;

        var (w1, l1) = ScoreFromWinningBase(baseWin, lov);
        var (w2, l2) = ScoreFromLosingBase(baseLose, lov);

        int winScore  = (int)Math.Round((w1 + w2) / 2.0);
        int loseScore = (int)Math.Round((l1 + l2) / 2.0);

        return homeWins ? (winScore, loseScore) : (loseScore, winScore);
    }

    private static (double winning, double losing) ScoreFromWinningBase(double baseWin, double lov)
    {
        double losing  = Math.Round(baseWin - lov * Math.Sqrt(baseWin));
        double winning = Math.Round(baseWin);
        if (winning == losing) winning++;
        if (losing < 0) losing = 0;
        return (winning, losing);
    }

    private static (double winning, double losing) ScoreFromLosingBase(double baseLose, double lov)
    {
        double winning = Math.Round((lov * lov + 2 * baseLose + lov * Math.Sqrt(lov * lov + 4 * baseLose)) / 2.0);
        double losing  = Math.Round(baseLose);
        if (winning == losing) winning++;
        if (losing < 0) losing = 0;
        return (winning, losing);
    }
}
