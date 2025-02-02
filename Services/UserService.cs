using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class UserService
    {
        private static UserRepository repo => new UserRepository();

        public static Task<bool> DoesUserWithFieldExist<TValue>(string field, TValue value)
        {
            return repo.DoesUserWithFieldExist(field, value);
        }

        public static Task<List<MiniUser>> GetUsersByField<TValue>(string field, TValue value)
        {
            return repo.GetUsersByField(field, value);
        }
    }
}
