using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class VerificationCodeRepository : Repository
    {
        protected override string CollectionName => "verificationCodes";

        public Task<List<VerificationCode>> GetAllCodes()
        {
            return MongoService.GetEntireCollection<VerificationCode>(Collection);
        }

        public Task<List<VerificationCode>> GetCodesByField<TValue>(string field, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, field, value, VerificationCode.FromBsonDocument);
        }

        public Task<List<VerificationCode>> GetCodesByFilter(FilterDefinition<BsonDocument> filter)
        {
            return MongoService.GetEntitiesByFilter(Collection, filter, VerificationCode.FromBsonDocument);
        }

        public Task<ReplaceOneResult> UpsertCode(VerificationCode code)
        {
            return MongoService.UpsertEntity(Collection, code);
        }

        public Task<bool> DeleteCode(string codeId)
        {
            return MongoService.DeleteEntity(Collection, codeId);
        }
    }
}
