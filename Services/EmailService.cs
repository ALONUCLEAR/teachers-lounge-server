namespace teachers_lounge_server.Services
{
    public class EmailService
    {
        public async static Task SendMailToAddress(string emailAddress, string content) {
            string fullContent = $"{content}\n\n\nהודעה זו נשלחה על ידי מערכת חדר מורים";
        }

        // TODO: add validation to check if a user with this email already exists
        public async static Task<string> SendCodeToAddress(string emailAddress, int length = 8, string possibleChars = "")
        {
            string code = possibleChars.Length > 0 ? possibleChars.GenerateCode(length) : Utils.GenerateCode(length);
            // Email code to address
            string mailContent = $"זהו קוד למערכת חדר מורים. הקוד תקף למספר דקות בלבד.\n{code}";
            await SendMailToAddress(emailAddress, code);

            return code;
        }
    }
}
