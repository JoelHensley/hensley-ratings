using System;

namespace RatingSystem
{
    public class RatingSettings
    {
        public bool ComputeTeamRatings = true;
        public bool ComputeConferenceRatings = true;
        public bool ComputeDivisionRatings = true;
        public bool ComputeStandardRatings;
        public bool ComputeHomefieldAdvantageRatings;
        public bool ComputeMaxPointDifferentialRatings;
        public bool ComputeHensleyRatings;
        public int MaxPointDifferential = 14;
        public double LowerBounds = 1.0;
        public double UpperBounds = 4.0;
        public string TeamResultsOutputFile = "Output/TeamResults";
        public string ConferenceResultsOutputFile = "Output/ConferenceResults";
        public string DivisionResultsOutputFile = "Output/DivisionResults";
        public int Year;
        public int MinWeek = 4;
        public int MinGroupSize = 20;
        public int CurrentWeek;
        public DateTime CurrentCutoffDate;

        public RatingSettings()
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable("SEASON_YEAR"), out Year))
                Year = DateTime.Now.Year;
            if (int.TryParse(Environment.GetEnvironmentVariable("MIN_WEEK"), out int mw))
                MinWeek = mw;
            if (int.TryParse(Environment.GetEnvironmentVariable("MIN_GROUP_SIZE"), out int minG))
                MinGroupSize = minG;

            ComputeHensleyRatings              = IsEnabled("HENSLEY_RATING_ENABLED");
            ComputeStandardRatings             = IsEnabled("STANDARD_RATING_ENABLED");
            ComputeMaxPointDifferentialRatings = IsEnabled("MAX_POINT_DIFF_RATING_ENABLED");
            ComputeHomefieldAdvantageRatings   = IsEnabled("HOME_FIELD_ADV_RATING_ENABLED");
        }

        private static bool IsEnabled(string envVar) =>
            (Environment.GetEnvironmentVariable(envVar) ?? "false")
                .Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
