namespace DatabaseLayer
{
    public class TeamAffiliation
    {
        public int ID { get; set; }
        public int TeamID { get; set; }
        public int ConferenceID { get; set; }
        public int Year { get; set; }
        public virtual Team Team { get; set; }
        public virtual Conference Conference { get; set; }
    }
}
