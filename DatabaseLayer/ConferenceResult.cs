namespace DatabaseLayer
{
    public class ConferenceResult
    {
        public int ID { get; set; }
        public int ConferenceID { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double StandardRating { get; set; }
        public double HomefieldAdvantageRating { get; set; }
        public double MaxPointDifferentialRating { get; set; }
        public double HensleyRating { get; set; }
        public virtual Conference Conference { get; set; }
    }
}
