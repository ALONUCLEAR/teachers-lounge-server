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
        private static UserRepository acceptedUsersRepository => new UserRepository();

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

        private async static Task<bool> doesUserWithGovIdExist(string govId)
        {
            return await repo.DoesUserWithFieldExist("govId", govId)
                || await acceptedUsersRepository.DoesUserWithFieldExist("govId", govId);
        }

        private async static Task<string> doesUserWithEmailExist(string email)
        {
            if (await repo.DoesUserWithFieldExist("email", email))
            {
                return "קיימת כבר בקשה ליצירת משתמש עם המייל הזה.\nפנו לגורם המאשר כדי שיאשר את הצטרפותכם למערכת.";
            }

            string userStatus = await acceptedUsersRepository.GetUserWithFieldValue("email", email);
            if (userStatus.Length > 0)
            {
                switch (userStatus)
                {
                    case "inactive":
                        return "המשתמש המקושר לתיבת המייל הזו הושבת.\nעל מנת לשחזר אותו יש לשלוח בקשה לשחזור משתמש דרך המערכת.";
                    default:
                        return "קיים חשבון פעיל המקושר לתיבת המייל הזאת.\nאם שכחת את סיסמתך, קיים גם מסך \"שכחתי סיסמה\" במערכת.";
                }
            }
            return "";
        }

        private async static Task<bool> doesUserExist(UserRequest request)
        {
            string userWithEmailMessageEnd = await doesUserWithEmailExist(request.email);

            if (userWithEmailMessageEnd.Length >= 0)
            {
                EmailService.SendMailToAddress(request.email, $"זיהינו שניסת להרשם עם כתובת המייל הזאת.\n{userWithEmailMessageEnd}");

                return true;
            }

            return await doesUserWithGovIdExist(request.govId);
        }
        private static UserRequest SerializeUserRequest(UserRequest rawRequest)
        {
            if (!AreComplexFieldsValid(rawRequest))
            {
                throw new Exception("Invalid user request input");
            }

            UserRequest serializedRequest = new UserRequest(rawRequest);

            using (HashAlgorithm sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(rawRequest.password);
                var hashedBytes = sha256.ComputeHash(passwordBytes);
                serializedRequest.password = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }

            return serializedRequest;
        }
        public static Task<List<UserRequest>> GetAllUserRequests()
        {
            return repo.GetAllUserRequests();
        }
        public static async Task<int> CreateUserRequest(UserRequest userRequest)
        {
            if (!AreComplexFieldsValid(userRequest))
            {
                return StatusCodes.Status400BadRequest;
            }

            if (await doesUserExist(userRequest))
            {
                return StatusCodes.Status200OK;
            }

            UserRequest serializedInput = SerializeUserRequest(userRequest);
            await repo.CreateUserRequest(serializedInput);

            string welcomeMessage = $"{userRequest.info.firstName} {userRequest.info.lastName},\n" +
                $"כמה כיף לראות שפתחת בקשת השתתפות למערכת!\n" +
                $"הבקשה נקלטה במערכת ותופץ בדקות הקרובות למאשרים הרלוונטיים.\n" +
                $"כשהבקשה תאושר ישלח מייל לידע אותך שאפשר להתחיל להשתמש במערכת.\n" +
                $"שיהיה לך יום קסום!";
            await EmailService.SendMailToAddress(serializedInput.email, welcomeMessage);

            return StatusCodes.Status200OK;
        }

        public static Task<bool> DeleteUserRequest(string userRequestId)
        {
            return repo.DeleteUserRequest(userRequestId);
        }
    }
}
