using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class UserRequestRepository : Repository
    {
        protected override string CollectionName => "userRequests";

        public Task<List<UserRequest>> GetAllUserRequests()
        {
            return MongoService.GetEntireCollection<UserRequest>(Collection);
        }

        public Task<bool> DoesUserRequestWithFieldExist<TValue>(string field, TValue value)
        {
            return MongoService.DoesEntityWithFieldExist(Collection, field, value);
        }

        public Task<List<UserRequest>> GetUserRequestByField<TValue>(string field, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, field, value, UserRequest.FromBsonDocument);
        }
        public Task CreateUserRequest(UserRequest userRequest)
        {
            return MongoService.CreateEntity(Collection, userRequest);
        }

        public Task<bool> DeleteUserRequest(string userRequestId)
        {
            return MongoService.DeleteEntity(Collection, userRequestId);
        }
    }
}
