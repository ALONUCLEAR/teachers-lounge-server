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

            // TODO: when there's an association service, validate stuff here as well.
            // Until then, no associations for anyone!
            var allUserAssociations = validUser.associations.ShallowClone();
            var validAssociations = new List<ObjectId>();
            validUser.associations = validAssociations.ToArray().Map(objId => objId.ToString());

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

        public Task<bool> DeleteUser(string UserId)
        {
            return MongoService.DeleteEntity(Collection, UserId);
        }
    }
}
