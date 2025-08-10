using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class SchoolService
    {
        private static SchoolRepository repo => new SchoolRepository();
        public static Task<List<School>> GetAllSchools()
        {
            return repo.GetAllSchools();
        }

        public static async Task<School?> GetSchoolById(ObjectId id)
        {
            List<School> schools = (await repo.GetSchoolsByField("_id", id));

            return schools.Count == 1 ? schools[0] : null;
        }

        public static Task<List<ObjectId>> GetExistingSchoolIds(string[] schoolIds)
        {
            return repo.GetExistingSchoolIds(schoolIds);
        }
        public static Task<ReplaceOneResult> UpsertSchool(School school)
        {
            return repo.UpsertSchool(school);
        }
        public static Task<bool> DeleteSchool(string schoolId)
        {
            return repo.DeleteSchool(schoolId);
        }
    }
}
