using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using teachers_lounge_server.Entities;

namespace teachers_lounge_server.Services
{
    public class MailInput
    {
        public string title { get; set; }
        public string content { get; set; }

        public MailInput(string title, string content)
        {
            this.title = title;
            this.content = content;
        }
    }
    public class EmailService
    {
        private static string senderEmail = Environment.GetEnvironmentVariable("SENDER_EMAIL") ?? "";
        /// <summary>
        /// An app password is like an access token that email providers like gmail and outlook let us generate on our own and use instead of a password
        /// </summary>
        private static string senderAppPassword = Environment.GetEnvironmentVariable("SENDER_APP_PASSWORD") ?? "";
        private static string SMTP_HOST = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        private static int SMTP_PORT = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "465");

        public static Task SendMailToAddresses(string[] emailAddresses, MailInput input)
        {
            return SendMailToAddresses(emailAddresses, input.title, input.content);
        }

        public async static Task SendMailToAddresses(string[] emailAddresses, string title, string content)
        {
            string fullContent = $"{content}\n\n\nהודעה זו נשלחה על ידי מערכת חדר מורים";
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Teachers Lounge", senderEmail));
            message.To.AddRange(emailAddresses.Map(address => new MailboxAddress(address, address)));
            message.Subject = title;
            message.Body = new TextPart(TextFormat.Plain) { Text = fullContent };

            using (SmtpClient smptClient = new SmtpClient())
            {
                await smptClient.ConnectAsync(SMTP_HOST, SMTP_PORT, true);
                await smptClient.AuthenticateAsync(senderEmail, senderAppPassword);
                await smptClient.SendAsync(message);
            }
        }

        public async static Task SendMailByAssociations(string[] associationIds, string title, string content)
        {
            var users = await AssociationService.GetAllUsersInAssociations(associationIds);

            var emailAddresses = users.Map(user => user.email).ToArray();

            await SendMailToAddresses(emailAddresses, title, content);
        }

        public async static Task<string> SendCodeToAddress(string govId, string emailAddress, int length = 8, string possibleChars = "")
        {
            string code = possibleChars.Length > 0 ? possibleChars.GenerateCode(length) : Utils.GenerateCode(length);
            var existingCodes = await VerificationCodeService.GetCodesByGovId(govId);
            var newCode = new VerificationCode(govId, code);

            if (existingCodes.Count > 0)
            {
                newCode.id = existingCodes[0].id;
            }

            await VerificationCodeService.UpsertCode(newCode);
            // Email code to address
            string mailContent = $"הקוד תקף למספר דקות בלבד.\n{code}";
            await SendMailToAddresses([emailAddress], "קוד למערכת חדר מורים", code);

            return code;
        }

        public async static Task<string> SendCodeByGovId(string govId, int length = 8, string possibleChars = "")
        {
            var usersWithThisGovId = await UserService.GetUsersByField("govId", govId);

            if (usersWithThisGovId.Count != 1)
            {
                // Act to the user as if the request succeeded
                string code = possibleChars.Length > 0 ? possibleChars.GenerateCode(length) : Utils.GenerateCode(length);

                return code;
            }

            return await SendCodeToAddress(govId, usersWithThisGovId[0].email, length, possibleChars);
        }

        public async static Task SendStatusBasedMessageToUser(MiniUser user)
        {
            if (user.activityStatus == ActivityStatus.Active)
            {
                return;
            }

            string title = "נסיון התחברות למערכת חדר מורים";
            string userState = user.activityStatus == ActivityStatus.Blocked ? "חסום" : "ממתין לאישור";
            string revertingAction = user.activityStatus == ActivityStatus.Blocked ? "עד שישוחזר" : "לאישור הבקשה";

            string body = $"זיהינו שניסית להתחבר למערכת חדר מורים. המשתמש שלך {userState}.\n"
                + $"נא להמתין {revertingAction} ע\"י הגורמים המאשרים הרלוונטיים.";
            MailInput mail = new(title, body);
            await SendMailToAddresses([user.email], mail);
        }
    }
}
