using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.Mail;
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

        private async static Task<bool> DoesUserExist(UserRequest request, bool sendMailIfUserExists = false)
        {
            string userWithEmailMessageEnd = await GetEmailTextBasedOnUserStatus(request.email);

            if (userWithEmailMessageEnd.Length > 0)
            {
                if (sendMailIfUserExists)
                {
                    await EmailService.SendMailToAddresses([request.email], "נסיון הרשמה - מערכת חדר מורים", $"זיהינו שניסת להרשם עם כתובת המייל הזאת.\n{userWithEmailMessageEnd}");
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

        public async static Task<bool> CanUserAffectRequest(string? userId, string requestId)
        {
            List<UserRequest> targetUsers = await repo.GetUserRequestByField("_id", ObjectId.Parse(requestId));

            if (targetUsers.Count != 1)
            {
                return false;
            }

            string[] roles = await UserService.GetRelevantRolesByUserId(userId);
            string targetRole = targetUsers[0].role;

            return roles.Some(role => role == targetRole);
        }

        public async static Task<List<UserRequest>> GetAllRelevantUserRequests(string? userId)
        {
            FilterDefinition<BsonDocument> relavenceFilter = await UserService.GetRoleBasedFilter(userId);

            return RemovePassword(await repo.GetUserReqeuestsByFilter(relavenceFilter));
        }

        public async static Task<List<User>> GetAllRequestsForSchool(string userId, string schoolId)
        {
            List<FilterDefinition<BsonDocument>> relavenceFilters = new();
            relavenceFilters.Add(await UserService.GetRoleBasedFilter(userId));
            relavenceFilters.Add(Builders<BsonDocument>.Filter.AnyEq("associatedSchools", schoolId));

            return RemovePassword(await repo.GetUserReqeuestsByMultipleFilters(relavenceFilters)).Map(request => new User(request));
        }
        public static async Task<int> CreateUserRequest(UserRequest userRequest)
        {
            if (!AreComplexFieldsValid(userRequest))
            {
                return StatusCodes.Status400BadRequest;
            }

            if (await DoesUserExist(userRequest, true))
            {
                return StatusCodes.Status200OK;
            }


            string[] nonSuperRoles = { Role.Base, Role.Admin };
            string alertBody = $"המשתמש {userRequest.info.fullName}({userRequest.govId}) מבקש להצטרף למערכת.\n"
                + $"ההרשאה שבוקשה היא {Role.HebrewFromKey(userRequest.role)}\n";

            bool isSuper = !nonSuperRoles.Contains(userRequest.role);
            ObjectId schoolId = ObjectId.Empty;

            if (!isSuper)
            {
                if (userRequest.associatedSchools.Length < 1 || !ObjectId.TryParse(userRequest.associatedSchools[0], out schoolId))
                {
                    return StatusCodes.Status400BadRequest;
                }

                var school = await SchoolService.GetSchoolById(schoolId);

                if (school == null)
                {
                    return StatusCodes.Status400BadRequest;
                }

                alertBody += $"ובית הספר הוא {school.name}({school.municipality.name})";
            }

            UserRequest serializedInput = SerializeUserRequest(userRequest);
            await repo.CreateUserRequest(serializedInput);

            string welcomeMessage = $"{userRequest.info.fullName},\n" +
                $"כמה כיף לראות שפתחת בקשת השתתפות למערכת!\n" +
                $"הבקשה נקלטה במערכת ותופץ בדקות הקרובות למאשרים הרלוונטיים.\n" +
                $"כשהבקשה תאושר ישלח מייל לידע אותך שאפשר להתחיל להשתמש במערכת.\n" +
                $"שיהיה לך יום קסום!";
            await EmailService.SendMailToAddresses([serializedInput.email], "בקשת יצירת משתמש", welcomeMessage);

            string acceptancePath = isSuper ? "user-status-management" : "teacher-management?pending=true";

            Alert newUserToAcceptAlert = new Alert()
            {
                title = "משתמש חדש מבקש את אישורך להצטרף",
                body = alertBody,
                importanceLevel = isSuper ? ImportanceLevel.Urgent : ImportanceLevel.High,
                link = $"{Utils.CLIENT_BASE_URL}/#/{acceptancePath}",
                dateCreated = DateTime.Now,
            };

            if (isSuper)
            {
                newUserToAcceptAlert = await AlertService.FillAlertByRoles(newUserToAcceptAlert, [Role.Support]);
            } else if (userRequest.role == Role.Admin)
            {
                newUserToAcceptAlert = await AlertService.FillAlertByRoles(newUserToAcceptAlert, [Role.SuperAdmin]);
            }
            else
            {
                newUserToAcceptAlert = await AlertService.FillAlertBySchool(newUserToAcceptAlert, schoolId);
            }

            await AlertService.SendAlert(newUserToAcceptAlert, true, null);

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

                await EmailService.SendMailToAddresses([userRequest.email], mailTitle, pendingUserMessage);

                return;
            }

            var usersWithGovId = await UserService.GetUsersByField("govId", govId);

            if (usersWithGovId.Count != 1)
            {
                // Somethings sus, pretend everything is alright
                return;
            }

            var user = usersWithGovId[0];

            string messageBody = user.activityStatus == ActivityStatus.Blocked
                ? "בקשת השחזור שלך נשלחה לגורמים הרלוונטיים"
                : "שמנו לב שניסית לשלוח בקשת שחזור למרות שהמשתמש שלך פעיל.\n" +
                  "בשביל מקרים של קשיים בהתחברות למשתמש יצרנו גם דף \"שכחתי סיסמה\" בקישור הבא\n" +
                  // TODO: actually put the index there
                  @$"{Utils.CLIENT_BASE_URL}/#/forgot-password";
            string message = $"שלום {user.info.fullName}.\n{messageBody}";

            if (user.activityStatus == ActivityStatus.Blocked)
            {
                string[] nonSuperRoles = { Role.Base, Role.Admin };
                string alertBody = $"המשתמש {user.info.fullName}({user.govId}) מבקש לבטל את השהייתו מהמערכת.\n"
                    + $"ההרשאה שהייתה לו היא {Role.HebrewFromKey(user.role)}\n";

                bool isSuper = !nonSuperRoles.Contains(user.role);

                Alert unblockUserAlert = new Alert()
                {
                    title = "משתמש מבקש שחזור",
                    body = alertBody,
                    importanceLevel = isSuper ? ImportanceLevel.Urgent : ImportanceLevel.High,
                    link = $"{Utils.CLIENT_BASE_URL}/#/user-status-management",
                    dateCreated = DateTime.Now,
                };

                if (isSuper)
                {
                    unblockUserAlert = await AlertService.FillAlertByRoles(unblockUserAlert, [Role.Support]);
                }
                else
                {
                    unblockUserAlert = await AlertService.FillAlertByRoles(unblockUserAlert, [Role.SuperAdmin]);
                }

                await AlertService.SendAlert(unblockUserAlert, true, user.id);
            }

            await EmailService.SendMailToAddresses([user.email], mailTitle, message);
        }

        public static Task<bool> DeleteUserRequest(string userRequestId)
        {
            return repo.DeleteUserRequest(userRequestId);
        }
    }
}
