namespace DatabaseLayer
{
    public class ConferenceAffiliation
    {
        public int ID { get; set; }
        public int ConferenceID { get; set; }
        public int DivisionID { get; set; }
        public int Year { get; set; }
        public virtual Conference Conference { get; set; }
        public virtual Division Division { get; set; }
    }
}
