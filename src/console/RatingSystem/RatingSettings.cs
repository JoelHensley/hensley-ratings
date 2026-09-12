using System;

namespace RatingSystem
{
    public class RatingSettings
    {
        public bool ComputeTeamRatings = true;
        public bool ComputeConferenceRatings = true;
        public bool ComputeDivisionRatings = true;
        public bool ComputeStandardRatings = true;
        public bool ComputeHomefieldAdvantageRatings = true;
        public bool ComputeMaxPointDifferentialRatings = true;
        public bool ComputeHensleyRatings = true;
        public int MaxPointDifferential = 14;
        public double LowerBounds = 1.0;
        public double UpperBounds = 4.0;
        public string TeamResultsOutputFile = "Output/TeamResults";
        public string ConferenceResultsOutputFile = "Output/ConferenceResults";
        public string DivisionResultsOutputFile = "Output/DivisionResults";
        public int Year;
        public int Week;
        // Groups smaller than this are skipped — avoids extreme ratings from tiny isolated schools
        public int MinGroupSize = 20;

        public RatingSettings()
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable("SEASON_YEAR"), out Year))
                Year = DateTime.Now.Year;
            if (!int.TryParse(Environment.GetEnvironmentVariable("WEEK"), out Week))
                Week = 1;
            if (int.TryParse(Environment.GetEnvironmentVariable("MIN_GROUP_SIZE"), out int minG))
                MinGroupSize = minG;
        }
    }
}
