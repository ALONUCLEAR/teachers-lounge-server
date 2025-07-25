using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class UserService
    {
        private static UserRepository repo => new UserRepository();
        private const string SUPPORT_ASSOCIATED_SCHOOL = "SupportApproval";

        private static List<User> RemovePassword(List<User> rawData)
        {
            return rawData.Select(request => new User(request) { password = "How stupid do you think I am?" }).ToList();
        }
        public static Task<bool> DoesUserWithFieldExist<TValue>(string field, TValue value)
        {
            return repo.DoesUserWithFieldExist(field, value);
        }
        public async static Task<string> GetUserRole(string? userId)
        {
            if (userId == null || !userId.IsObjectId())
            {
                return "";
            }

            List<User> usersWithId = await GetUsersByField("_id", ObjectId.Parse(userId));

            return usersWithId.Count == 1 ? usersWithId[0].role : "";
        }
        private static string[] GetRelevantRoles(string userRole)
        {
            switch(userRole)
            {
                case Role.Support:
                    return Role.GetAllRoles();
                case Role.SuperAdmin:
                    return new string[] { Role.Base, Role.Admin };
                case Role.Admin:
                    return new string[] { Role.Base };
                default:
                    return new string[0];
            }
        }
        public async static Task<string[]> GetRelaventRolesByUserId(string? userId)
        {
            string userRole = await GetUserRole(userId);

            return GetRelevantRoles(userRole);
        }
        public async static Task<FilterDefinition<BsonDocument>> GetRoleBasedFilter(string? userId)
        {
            string[] relaventRoles = await GetRelaventRolesByUserId(userId);

            return Builders<BsonDocument>.Filter.In("role", relaventRoles);
        }
        public async static Task<List<User>> GetUsersByStatus(string? userId, string status)
        {
            if (!ActivityStatus.isValid(status))
            {
                return new List<User>();
            }

            var filterList = new List<FilterDefinition<BsonDocument>>();
            filterList.Add(await GetRoleBasedFilter(userId));
            filterList.Add(Builders<BsonDocument>.Filter.Eq("activityStatus", status));

            List<User> filteredUsers = await repo.GetUsersByMultipleFilters(filterList);

            return RemovePassword(filteredUsers);
        }
        public async static Task<List<User>> GetUsersByField<TValue>(string field, TValue value)
        {
            return RemovePassword(await repo.GetUsersByField(field, value));
        }

        public async static Task<int> CreateUserFromRequestId(string requestId)
        {
            if (requestId == null || !requestId.IsObjectId())
            {
                return StatusCodes.Status400BadRequest;
            }

            UserRequest request = await UserRequestService.GetFullUserRequestById(ObjectId.Parse(requestId));

            return await CreateUserFromRequest(request);
        }

        public async static Task<int> CreateUserFromRequest(UserRequest request)
        {
            if (request == null)
            {
                return StatusCodes.Status400BadRequest;
            }

            User userCopy = new User(request);

            userCopy.activityStatus = ActivityStatus.Active;
            
            if (userCopy.associatedSchools[0] == SUPPORT_ASSOCIATED_SCHOOL)
            {
                userCopy.associatedSchools = new string[0];
            }

            await repo.CreateUser(userCopy);

            string welcomeMessage = $"{userCopy.info.fullName},\n" +
                $"הבקשה שפתחת ליצירת משתמש אושרה.\n" +
                $"עכשיו ניתן להתחיל ולהשתמש במערכת בקישור הבא:\n" +
                @"https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            await EmailService.SendMailToAddress(userCopy.email, "אישור בקשה ליצירת משתמש", welcomeMessage);

            if (!await UserRequestService.DeleteUserRequest(request.id))
            {
                return StatusCodes.Status206PartialContent;
            }

            return StatusCodes.Status200OK;
        }

        public static Task<UpdateResult> UpdateUserByFields<TFieldValue, TNewValue>(
            string fieldToCheck, TFieldValue valueToCheck,
            string fieldToUpdate, TNewValue newValue)
        {
            return repo.UpdateUserByFields(fieldToCheck, valueToCheck, fieldToUpdate, newValue);
        }

        public async static Task<bool> CanRequestAffectUser(string requestingUserId, string targetUserId)
        {
            List<User> targetUsers = await GetUsersByField("_id", ObjectId.Parse(targetUserId));

            if (targetUsers.Count != 1)
            {
                return false;
            }

            string[] roles = await GetRelaventRolesByUserId(requestingUserId);
            string targetRole = targetUsers[0].role;

            return roles.Some(role => role == targetRole);
        }

        public static Task<UpdateResult> ChangeUserStatus(string userId, bool isActive)
        {
            if (!userId.IsObjectId())
            {
                throw new InvalidCastException($"The user id {userId} is not a valid object id");
            }

            string newStatus = isActive ? ActivityStatus.Active : ActivityStatus.Blocked;

            return UpdateUserByFields("_id", ObjectId.Parse(userId), "activityStatus", newStatus);
        }

        public static async Task<User?> GetUserByCredentials(string govId, string password)
        {
            List<User> usersByGovId = await repo.GetUsersByField("govId", govId);

            if (usersByGovId.Count != 1)
            {
                return null;
            }

            User found = usersByGovId[0];
            bool doPasswordsMatch = password.Hash().Equals(found.password);

            if (!doPasswordsMatch)
            {
                return null;
            }

            found.password = "";

            return found;
        }

        public static async Task SendChangePasswordEmail(string email, string userId)
        {
            await EmailService.SendMailToAddress(email, "שינוי סיסמא", $"במידה ואתה רוצה לשלות סיסממא תמצוץ לי את הביצה {userId}");
        }

        public static async Task<UpdateResult> ChangePassword(string userId, string newPassword)
        {
           return await UpdateUserByFields("_id", ObjectId.Parse(userId), "password", newPassword.Hash());
        }

        public static async Task<string> getUserIdByGovId(string govId)
        {
            var users = await repo.GetUsersByField("govId", govId);
            
            return users.Count == 1 ? users[0].id : null;
        }
    }
}
