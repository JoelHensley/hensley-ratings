namespace HensleyRatings.Api.Models;

public record RatedTeam(
    int TeamId,
    string Name,
    string ConferenceName,
    int ConferenceId,
    string DivisionName,
    int DivisionId,
    int Wins,
    int Losses,
    double HensleyRating,
    double ScheduleStrength,
    int RankOverall,
    int RankDivision,
    int RankConference,
    int? WeekOverWeekChange,
    int? PointsScored,
    int? PointsAllowed
);

public record ScheduleGameResponse(
    int GameId,
    string Date,
    int HomeTeamId,
    string HomeTeamName,
    string HomeTeamConference,
    int? HomeScore,
    int AwayTeamId,
    string AwayTeamName,
    string AwayTeamConference,
    int? AwayScore,
    bool IsNeutralSite,
    bool IsComplete,
    int? PredictedHomeScore,
    int? PredictedAwayScore,
    double? HomeTeamRating,
    double? AwayTeamRating,
    int? HomeTeamRank,
    int? AwayTeamRank,
    int? HomeTeamPtsScored,
    int? AwayTeamPtsScored
);

public record TeamGameResponse(
    int GameId,
    string Date,
    bool IsHome,
    bool IsNeutralSite,
    int OpponentId,
    string OpponentName,
    int? OpponentRank,
    int? TeamScore,
    int? OpponentScore,
    bool? IsWin,
    int RunningWins,
    int RunningLosses,
    double? TeamRating,
    int? TeamRank,
    int? TeamRankDelta,
    double? ScheduleStrength,
    int? ScheduleStrengthRank,
    int? ScheduleStrengthRankDelta,
    int? WeekNumber,
    int? PredictedTeamScore,
    int? PredictedOpponentScore
);

public record TeamDetailResponse(
    int TeamId,
    string Name,
    string ConferenceName,
    int ConferenceId,
    string DivisionName,
    int DivisionId,
    int Wins,
    int Losses,
    double HensleyRating,
    int RankOverall,
    double ScheduleStrength,
    int? PointsScored,
    int? PointsAllowed,
    IEnumerable<TeamGameResponse> Games,
    IEnumerable<ByeWeekResponse> ByeWeeks
);

public record ByeWeekResponse(
    int Week,
    int Wins,
    int Losses,
    double? Rating,
    int? RatingRank,
    int? RatingRankDelta,
    double? ScheduleStrength,
    int? ScheduleStrengthRank,
    int? ScheduleStrengthRankDelta
);

public record WeekOption(int Week, string CutoffDate, bool HasPrevious, bool HasRatings);
public record MetaWeeksResponse(IEnumerable<WeekOption> Weeks);
public record MetaYearsResponse(IEnumerable<int> Years);
public record DivisionResponse(int DivisionId, string Name);
public record ConferenceResponse(int ConferenceId, string Name, int DivisionId);
