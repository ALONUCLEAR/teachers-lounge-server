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
            return (await repo.GetUsersByField("govId", govId))[0].id;
        }
    }
}
