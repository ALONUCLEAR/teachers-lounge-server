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
            switch (userRole)
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
        public async static Task<string[]> GetRelevantRolesByUserId(string? userId)
        {
            string userRole = await GetUserRole(userId);

            return GetRelevantRoles(userRole);
        }
        public async static Task<FilterDefinition<BsonDocument>> GetRoleBasedFilter(string? userId)
        {
            return GetPureRoleBasedFilter(await GetRelevantRolesByUserId(userId));
        }

        public static FilterDefinition<BsonDocument> GetPureRoleBasedFilter(string[] relevantRoles)
        {
            return Builders<BsonDocument>.Filter.In("role", relevantRoles);
        }
        public async static Task<List<User>> GetUsersByStatus(string? userId, string status, bool affectedOnly)
        {
            if (!ActivityStatus.isValid(status))
            {
                return new List<User>();
            }

            var filterList = new List<FilterDefinition<BsonDocument>>();

            if (affectedOnly)
            {
                filterList.Add(await GetRoleBasedFilter(userId));
            }

            filterList.Add(Builders<BsonDocument>.Filter.Eq("activityStatus", status));

            List<User> filteredUsers = await repo.GetUsersByMultipleFilters(filterList);

            return RemovePassword(filteredUsers);
        }

        public async static Task<List<User>> GetUsersByRoles(string[] roles)
        {
            var validRoles = roles.Filter(Role.isValid);

            if (validRoles.Length < 1)
            {
                return new();
            }

            var filterList = new List<FilterDefinition<BsonDocument>>();
            filterList.Add(GetPureRoleBasedFilter(validRoles));
            filterList.Add(Builders<BsonDocument>.Filter.Eq("activityStatus", ActivityStatus.Active));

            List<User> filteredUsers = await repo.GetUsersByMultipleFilters(filterList);

            return RemovePassword(filteredUsers);
        }
        public async static Task<List<User>> GetUsersByField<TValue>(string field, TValue value)
        {
            return RemovePassword(await repo.GetUsersByField(field, value));
        }

        public async static Task<List<User>> GetUsersByFieldIn<TValue>(string field, TValue[] values)
        {
            return RemovePassword(await repo.GetUsersByFieldValueIn(field, values));
        }

        public async static Task<User?> GetUserById(string? userId)
        {
            if (userId == null)
            {
                return null;
            }

            var usersWithId = await GetUsersByField("_id", ObjectId.Parse(userId));

            return usersWithId.Count == 1 ? usersWithId[0] : null;
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
                $"{Utils.CLIENT_BASE_URL}/#/login";
            await EmailService.SendMailToAddresses([userCopy.email], "אישור בקשה ליצירת משתמש", welcomeMessage);

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

        public static Task<UpdateResult> UnlinkSchool(string targetUserId, string schoolId)
        {
            return repo.UnlinkSchool(ObjectId.Parse(targetUserId), ObjectId.Parse(schoolId));
        }

        public static Task<UpdateResult> LinkSchool(string[] targetUserIds, string schoolId)
        {
            return repo.LinkSchool(targetUserIds.Map(ObjectId.Parse), ObjectId.Parse(schoolId));
        }

        public async static Task<bool> CanRequestAffectUser(string requestingUserId, string targetUserId, string targetStatus = ActivityStatus.Active)
        {
            List<User> targetUsers = new();

            if (targetStatus == ActivityStatus.Pending)
            {
                targetUsers.Add(new User(await UserRequestService.GetFullUserRequestById(ObjectId.Parse(targetUserId))));
            } else
            {
                targetUsers.AddRange(await GetUsersByField("_id", ObjectId.Parse(targetUserId)));
            }

            if (targetUsers.Count != 1 || targetUsers[0].activityStatus != targetStatus)
            {
                return false;
            }

            string[] roles = await GetRelevantRolesByUserId(requestingUserId);
            string targetRole = targetUsers[0].role;

            return roles.Some(role => role == targetRole);
        }

        public static async Task<bool> HasPermissions(string? userId, string? requiredRole)
        {
            if (requiredRole == null)
            {
                return true;
            }

            var user = await GetUserById(userId);

            if (user == null || user.activityStatus != ActivityStatus.Active)
            {
                return false;
            }

            string[] lesserRoles = GetRelevantRoles(user.role);

            return requiredRole == user.role || lesserRoles.Some(role => role == requiredRole);
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
                List<UserRequest> openRequestsByGovId = await UserRequestService.GetUserRequestByField("govId", govId);

                if (openRequestsByGovId.Count > 0)
                {
                    try
                    {
                        await EmailService.SendStatusBasedMessageToUser(openRequestsByGovId[0]);
                    }
                    catch
                    {
                        // We don't want the process to fail just because we failed to send a mail
                    }
                }

                return null;
            }

            User found = usersByGovId[0];

            if (found.activityStatus != ActivityStatus.Active)
            {
                try
                {
                    await EmailService.SendStatusBasedMessageToUser(found);
                } catch
                {
                    // We don't want the process to fail just because we failed to send a mail
                }

                return null;
            }

            bool doPasswordsMatch = password.Hash().Equals(found.password);

            if (!doPasswordsMatch)
            {
                return null;
            }

            found.password = "";

            return found;
        }

        public static async Task<IEnumerable<User>> GetUsersBySchool(ObjectId schoolId)
        {
            var filterLists = new List<FilterDefinition<BsonDocument>>();
            filterLists.Add(Builders<BsonDocument>.Filter.AnyEq("associatedSchools", schoolId));
            filterLists.Add(Builders<BsonDocument>.Filter.Eq("activityStatus", ActivityStatus.Active));
            List<User> usersBySchoolId = await repo.GetUsersByMultipleFilters(filterLists);

            return RemovePassword(usersBySchoolId);
        }
        public static async Task SendChangePasswordEmail(string email, string userId)
        {
            await EmailService.SendMailToAddresses([email], "שינוי סיסמא", $"במידה ואתה רוצה לשלות סיסממא תמצוץ לי את הביצה {userId}");
        }

        public static Task<UpdateResult> ChangePassword(string userId, string newPassword)
        {
           return UpdateUserByFields("_id", ObjectId.Parse(userId), "password", newPassword.Hash());
        }

        public static async Task<User?> GetUserByGovId(string govId)
        {
            var users = await GetUsersByField("govId", govId);

            return users.Count == 1 ? users[0] : null;
        }

        public static async Task<List<string>> GetUserEmailAddresses(string[] userIds)
        {
            var users = await repo.GetUsersByFieldValueIn("_id", userIds.FilterAndMap(id => id.IsObjectId(), id => ObjectId.Parse(id)));

            return users.Map(user => user.email);
        }

        public static async Task<List<string>> GetUserEmailAddresses(List<string> userIds)
        {
            var users = await repo.GetUsersByFieldValueIn("_id", userIds.FilterAndMap(id => id.IsObjectId(), id => ObjectId.Parse(id)).ToArray());

            return users.Map(user => user.email);
        }
    }
}
