using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class SchoolService
    {
        public static Task<List<School>> getAllSchools()
        {
            return SchoolRepository.getAllSchools();
        }
    }
}
