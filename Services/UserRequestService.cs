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

        private async static Task<bool> doesUserWithEmailExist(string email)
        {
            return await repo.DoesUserWithFieldExist("email", email)
                || await acceptedUsersRepository.DoesUserWithFieldExist("email", email);
        }

        private async static Task<bool> doesUserExist(UserRequest request)
        {
            if (await doesUserWithEmailExist(request.email))
            {
                // TODO: send email to that email that it exists
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

            return StatusCodes.Status200OK;
        }

        public static Task<bool> DeleteUserRequest(string userRequestId)
        {
            return repo.DeleteUserRequest(userRequestId);
        }
    }
}
