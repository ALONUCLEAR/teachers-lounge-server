using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class UserRepository : Repository
    {
        protected override string CollectionName => "users";

        public Task<List<User>> GetUsersByField<TValue>(string field, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, field, value, User.FromBsonDocument);
        }

        public Task<List<User>> GetUsersByFieldValueIn<TValue>(string field, TValue[] values)
        {
            return MongoService.GetEntitiesByFieldValueIn(Collection, field, values, User.FromBsonDocument);
        }

        public Task<List<User>> GetUsersByFieldContainsValue<TValue>(string field, TValue filterValue)
        {
            return MongoService.GetEntitiesByFieldContainsValue(Collection, field, filterValue, User.FromBsonDocument);
        }

        public Task<List<User>> GetUsersByMultipleFilters(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            return MongoService.GetEntitiesByMultipleFilters(Collection, filterList, User.FromBsonDocument);
        }

        public Task<List<User>> GetUsersByFilter(FilterDefinition<BsonDocument> filter)
        {
            return MongoService.GetEntitiesByFilter(Collection, filter, User.FromBsonDocument);
        }

        public Task<bool> DoesUserWithFieldExist<TValue>(string field, TValue value)
        {
            return MongoService.DoesEntityWithFieldExist(Collection, field, value);
        }

        private async static Task<User> MakeUserRefsValid(User user)
        {
            User validUser = new User(user);

            var allUserSchoolIds = validUser.associatedSchools.ShallowClone();
            var validSchoolIds = await SchoolService.GetExistingSchoolIds(allUserSchoolIds);
            validUser.associatedSchools = validSchoolIds.ToArray().Map(objId => objId.ToString());

            return validUser;
        }

        public async Task CreateUser(User User)
        {
            await MongoService.CreateEntity(Collection, await MakeUserRefsValid(User));
        }

        public Task<UpdateResult> UpdateUserByFields<TFieldValue, TNewValue>(
            string fieldToCheck, TFieldValue valueToCheck,
            string fieldToUpdate, TNewValue newValue)
        {
            return MongoService.UpdateEntitiesByField(Collection, fieldToCheck, valueToCheck, fieldToUpdate, newValue, User.FromBsonDocument);
        }

        public Task<UpdateResult> UnlinkSchool(ObjectId userId, ObjectId schoolToUnlink)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", userId);
            var update = Builders<BsonDocument>.Update.Pull("associatedSchools", schoolToUnlink);

            return Collection.UpdateOneAsync(filter, update);
        }

        public Task<UpdateResult> LinkSchool(ObjectId[] userIds, ObjectId schoolToLink)
        {
            var filter = Builders<BsonDocument>.Filter.In("_id", userIds);
            var update = Builders<BsonDocument>.Update.AddToSet("associatedSchools", schoolToLink);

            return Collection.UpdateManyAsync(filter, update);
        }

        public Task<bool> DeleteUser(string UserId)
        {
            return MongoService.DeleteEntity(Collection, UserId);
        }
    }
}
