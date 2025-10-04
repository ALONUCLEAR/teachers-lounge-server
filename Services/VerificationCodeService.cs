using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class VerificationCodeService
    {
        private static VerificationCodeRepository repo => new VerificationCodeRepository();

        public static Task<List<VerificationCode>> GetAllCodes()
        {
            return repo.GetAllCodes();
        }

        public static async Task<VerificationCode?> GetCodeById(ObjectId id)
        {
            var codes = await repo.GetCodesByField("_id", id);
            return codes.Count == 1 ? codes[0] : null;
        }

        public static Task<ReplaceOneResult> UpsertCode(VerificationCode code)
        {
            return repo.UpsertCode(code);
        }

        public static Task<bool> DeleteCode(string codeId)
        {
            return repo.DeleteCode(codeId);
        }

        public static Task<List<VerificationCode>> GetCodesByGovId(string govId)
        {
            return repo.GetCodesByField("govId", govId);
        }

        public async static Task<bool> IsCodeVerified(string govId, string code)
        {
            var now = DateTime.Now;
            var validCodes = (await GetCodesByGovId(govId)).Filter(code => code.expiryDate >= now).ToArray();

            if (validCodes.Length < 1)
            {
                return false;
            }

            bool isCorrect = code == validCodes[0].code;

            if (isCorrect)
            {
                await DeleteCode(validCodes[0].id);
            }

            return isCorrect;
        }

        public static Task<List<VerificationCode>> GetCodesExpiringBefore(DateTime expiryTime)
        {
            return repo.GetCodesByFilter(Builders<BsonDocument>.Filter.Lt("expiryDate", expiryTime));
        }
    }
}
