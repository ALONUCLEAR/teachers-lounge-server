namespace teachers_lounge_server.Entities
{
    public struct ImportanceLevel
    {
        public const string Low = "Low";
        public const string Medium= "Medium";
        public const string High = "High";
        public const string Urgent = "Urgent";

        public static bool isValid(string maybeImportanceLevel)
        {
            return typeof(ImportanceLevel).GetFields().Some(field => field.Name.Equals(maybeImportanceLevel));
        }
    }
    // TODO: implement when needed
    public class Alert
    {
    }
}
