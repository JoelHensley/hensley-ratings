/* RatingSettings.cs
 * Joel Hensley
 * January 16, 2010
 * This class contains the settings for the rating system.
 */
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
    }
}
