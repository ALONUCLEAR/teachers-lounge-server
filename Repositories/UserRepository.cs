using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class UserRepository : Repository
    {
        protected override string CollectionName => "users";

        //public Task<List<User>> GetAllUsers()
        //{
        //    return MongoService.GetEntireCollection<User>(Collection);
        //}

        // TODO: change to user object so I can get their status
        public Task<List<MiniUser>> GetUsersByField<TValue>(string field, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, field, value, MiniUser.FromBsonDocument);
        }

        public Task<bool> DoesUserWithFieldExist<TValue>(string field, TValue value)
        {
            return MongoService.DoesEntityWithFieldExist(Collection, field, value);
        }

        //public Task CreateUser(User User)
        //{
        //    return MongoService.CreateEntity(Collection, User);
        //}

        public Task<bool> DeleteUser(string UserId)
        {
            return MongoService.DeleteEntity(Collection, UserId);
        }
    }
}
