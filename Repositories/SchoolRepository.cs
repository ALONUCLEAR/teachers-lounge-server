using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class SchoolRepository: Repository
    {
        protected override string CollectionName => "schools";
        public Task<List<School>> GetAllSchools()
        {
            return MongoService.GetEntireCollection<School>(Collection);
        }
        public Task<ReplaceOneResult> UpsertSchool(School school)
        {
            return MongoService.UpsertEntity(Collection, school);
        }
    }
}
