namespace teachers_lounge_server.Entities
{
    public class GovernmentData
    {
        public int id { get; set; }
        public string name { get; set; }
        public int fk { get; set; }

        public GovernmentData(int id, string name, int fk)
        {
            this.id = id;
            this.name = name;
            this.fk = fk;
        }

        public GovernmentData(GovernmentData toCopy)
        {
            this.id = toCopy.id;
            this.name = toCopy.name;
            this.fk = toCopy.fk;
        }
    }
}
