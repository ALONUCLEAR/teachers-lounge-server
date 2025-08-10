using MongoDB.Bson;
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

        public Task<List<School>> GetSchoolsByField<TValue>(string field, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, field, value, School.FromBsonDocument);
        }
        public Task<List<ObjectId>> GetExistingSchoolIds(string[] schoolIds)
        {
            ObjectId[] validIds = schoolIds.FilterAndMap(id => id.IsObjectId(), id => ObjectId.Parse(id));

            return MongoService.GetExistingValues(Collection, "_id", validIds, doc => doc.GetValue("_id").AsObjectId);
        }

        public Task<ReplaceOneResult> UpsertSchool(School school)
        {
            return MongoService.UpsertEntity(Collection, school);
        }
        public Task<bool> DeleteSchool(string schoolId)
        {
            return MongoService.DeleteEntity(Collection, schoolId);
        }
    }
}
