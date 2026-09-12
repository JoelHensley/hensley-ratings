namespace DatabaseLayer
{
    public class DivisionResult
    {
        public int ID { get; set; }
        public int DivisionID { get; set; }
        public int Year { get; set; }
        public int Week { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double StandardRating { get; set; }
        public double HomefieldAdvantageRating { get; set; }
        public double MaxPointDifferentialRating { get; set; }
        public double HensleyRating { get; set; }
        public double ScheduleStrength { get; set; }
        public virtual Division Division { get; set; }
    }
}
