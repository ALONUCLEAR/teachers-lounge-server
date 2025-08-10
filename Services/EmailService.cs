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
        public static Task SendMailToAddresses(string[] emailAddresses, MailInput input)
        {
            return SendMailToAddress(emailAddresses, input.title, input.content);
        }

        public async static Task SendMailToAddress(string[] emailAddresses, string title, string content) {
            string fullContent = $"{content}\n\n\nהודעה זו נשלחה על ידי מערכת חדר מורים";
            // TODO: use the title and actually send mail
        }

        public async static Task SendMailByAssociations(string[] associationIds, string title, string content)
        {
            var users = await AssociationService.GetAllUsersInAssociations(associationIds);

            var emailAddresses = users.Map(user => user.email).ToArray();

            await SendMailToAddress(emailAddresses, title, content);
        }

        public async static Task<string> SendCodeToAddress(string emailAddress, int length = 8, string possibleChars = "")
        {
            string code = possibleChars.Length > 0 ? possibleChars.GenerateCode(length) : Utils.GenerateCode(length);
            // Email code to address
            string mailContent = $"הקוד תקף למספר דקות בלבד.\n{code}";
            await SendMailToAddress([emailAddress], "קוד למערכת חדר מורים", code);

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

            return await SendCodeToAddress(usersWithThisGovId[0].email, length, possibleChars);
        }
    }
}
