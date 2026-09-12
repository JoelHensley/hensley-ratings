namespace DatabaseLayer
{
    public class TeamResult
    {
        public int ID { get; set; }
        public int TeamID { get; set; }
        public int Year { get; set; }
        public int Week { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double StandardRating { get; set; }
        public double HomefieldAdvantageRating { get; set; }
        public double MaxPointDifferentialRating { get; set; }
        public double HensleyRating { get; set; }
        public double ScheduleStrength { get; set; }
        public int? PointsScored { get; set; }
        public int? PointsAllowed { get; set; }
        public virtual Team Team { get; set; }
    }
}
