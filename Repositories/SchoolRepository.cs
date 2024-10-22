using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class SchoolRepository
    {
        private const string collectionName = "schools";
        public static Task<List<School>> getAllSchools()
        {
            return MongoService.getEntireCollection<School>(collectionName);
        }
    }
}
