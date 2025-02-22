using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class UserRequestService
    {
        private static UserRequestRepository repo => new UserRequestRepository();

        private static List<UserRequest> RemovePassword(List<UserRequest> rawData)
        {
            return rawData.Select(request => new UserRequest(request) { password = "How stupid do you think I am?" }).ToList();
        }

        public static bool AreComplexFieldsValid(UserRequest request)
        {
            try
            {
                new MailAddress(request.email);
            }
            catch (Exception e)
            {
                return false;
            }

            if (!Role.isValid(request.role))
            {
                return false;
            }

            return true;
        }

        private async static Task<bool> DoesUserWithGovIdExist(string govId)
        {
            return await repo.DoesUserRequestWithFieldExist("govId", govId)
                || await UserService.DoesUserWithFieldExist("govId", govId);
        }

        private async static Task<string> GetEmailTextBasedOnUserStatus(string email)
        {
            if (await repo.DoesUserRequestWithFieldExist("email", email))
            {
                return "קיימת כבר בקשה ליצירת משתמש עם המייל הזה.\nפנו לגורם המאשר כדי שיאשר את הצטרפותכם למערכת.";
            }

            List<User> existingUsers = await UserService.GetUsersByField("email", email);

            if (existingUsers.Count > 0)
            {
                switch (existingUsers[0].activityStatus)
                {
                    case ActivityStatus.Blocked:
                        return "המשתמש המקושר לתיבת המייל הזו הושבת.\nעל מנת לשחזר אותו יש לשלוח בקשה לשחזור משתמש דרך המערכת.";
                    default:
                        return "קיים חשבון פעיל המקושר לתיבת המייל הזאת.\nאם שכחת את סיסמתך, קיים גם מסך \"שכחתי סיסמה\" במערכת.";
                }
            }

            return "";
        }

        private async static Task<bool> DoesUserExist(UserRequest request, bool sendMailIfUserExists = true)
        {
            string userWithEmailMessageEnd = await GetEmailTextBasedOnUserStatus(request.email);

            if (userWithEmailMessageEnd.Length > 0)
            {
                if (sendMailIfUserExists)
                {
                    await EmailService.SendMailToAddress(request.email, "נסיון הרשמה - מערכת חדר מורים", $"זיהינו שניסת להרשם עם כתובת המייל הזאת.\n{userWithEmailMessageEnd}");
                }

                return true;
            }

            return await DoesUserWithGovIdExist(request.govId);
        }
        public async static Task<UserRequest> GetFullUserRequestById(ObjectId requestId)
        {
            return (await repo.GetUserRequestByField("_id", requestId))[0];
        }
        public async static Task<List<UserRequest>> GetUserRequestByField<TValue>(string field, TValue value)
        {
            return RemovePassword(await repo.GetUserRequestByField(field, value));
        }
        private static UserRequest SerializeUserRequest(UserRequest rawRequest)
        {
            if (!AreComplexFieldsValid(rawRequest))
            {
                throw new Exception("Invalid user request input");
            }

            UserRequest serializedRequest = new UserRequest(rawRequest);

            serializedRequest.password = rawRequest.password.Hash();

            return serializedRequest;
        }
        public async static Task<List<UserRequest>> GetAllUserRequests()
        {
            return RemovePassword(await repo.GetAllUserRequests());
        }
        public static async Task<int> CreateUserRequest(UserRequest userRequest)
        {
            if (!AreComplexFieldsValid(userRequest))
            {
                return StatusCodes.Status400BadRequest;
            }

            if (await DoesUserExist(userRequest))
            {
                return StatusCodes.Status200OK;
            }

            UserRequest serializedInput = SerializeUserRequest(userRequest);
            await repo.CreateUserRequest(serializedInput);

            string welcomeMessage = $"{userRequest.info.fullName},\n" +
                $"כמה כיף לראות שפתחת בקשת השתתפות למערכת!\n" +
                $"הבקשה נקלטה במערכת ותופץ בדקות הקרובות למאשרים הרלוונטיים.\n" +
                $"כשהבקשה תאושר ישלח מייל לידע אותך שאפשר להתחיל להשתמש במערכת.\n" +
                $"שיהיה לך יום קסום!";
            await EmailService.SendMailToAddress(serializedInput.email, "בקשת יצירת משתמש", welcomeMessage);
            // TODO: send alerts(+mails) to the group of relavent approvers

            return StatusCodes.Status200OK;
        }

        public static async Task SendUserRecoveryRequest(string govId)
        {
            var userRequestsWithGovId = await GetUserRequestByField("govId", govId);
            string mailTitle = "בקשת שחזור משתמש";

            if (userRequestsWithGovId.Count > 0)
            {
                var userRequest = userRequestsWithGovId[0];
                string pendingUserMessage = $"שלום {userRequest.info.fullName}.\n" +
                    $"שמנו לב שניסית לשלוח בקשת שחזור למערכת.\n" +
                    $"בקשת שחזור נועדה למשתמשים שנחסמו. שלך עדיין לא אושר אישור ראשוני\n" +
                    $"אנא פנה לגורמים הרלוונטיים כדי שיאשרו את הרשמתך";

                await EmailService.SendMailToAddress(userRequest.email, mailTitle, pendingUserMessage);

                return;
            }

            var usersWithGovId = await UserService.GetUsersByField("govId", govId);

            if (usersWithGovId.Count != 1)
            {
                // Somethings sus, pretend everything is alright
                return;
            }

            var user = usersWithGovId[0];

            string fullName = "there";//TODO: replace with actual fullName when the user class is implemented
            string messageBody = user.activityStatus == ActivityStatus.Blocked
                ? "בקשת השחזור שלך נשלחה לגורמים הרלוונטיים"
                : "שמנו לב שניסית לשלוח בקשת שחזור למרות שהמשתמש שלך פעיל.\n" +
                  "בשביל מקרים של קשיים בהתחברות למשתמש יצרנו גם דף \"שכחתי סיסמה\" בקישור הבא\n" +
                  @"https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            string message = $"שלום {fullName}.\n${messageBody}";

            await EmailService.SendMailToAddress(user.email, mailTitle, message);
        }

        public static Task<bool> DeleteUserRequest(string userRequestId)
        {
            return repo.DeleteUserRequest(userRequestId);
        }
    }
}
