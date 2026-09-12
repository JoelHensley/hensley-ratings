using System.Collections.Generic;

namespace DatabaseLayer
{
    public class Division
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int? Group { get; set; }
        public virtual ICollection<Conference> Conferences { get; set; }
    }
}
