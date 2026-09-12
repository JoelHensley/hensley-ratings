using System.Collections.Generic;

namespace DatabaseLayer
{
    public class Conference
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int DivisionID { get; set; }
        public int? Group { get; set; }
        public virtual Division Division { get; set; }
        public virtual ICollection<Team> Teams { get; set; }
        public virtual ICollection<ConferenceAffiliation> ConferenceAffiliations { get; set; }
    }
}
