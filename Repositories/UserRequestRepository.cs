using MongoDB.Driver;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
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

        public Task<bool> DoesUserWithFieldExist<TValue>(string field, TValue value)
        {
            return MongoService.DoesEntityWithFieldExist(Collection, field, value);
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
