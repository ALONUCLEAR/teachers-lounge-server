using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class AlertService
    {
        private static AlertRepository repo => new AlertRepository();

        public static Task<List<Alert>> GetAllAlerts()
        {
            return repo.GetAllAlerts();
        }

        public static async Task<Alert?> GetAlertById(ObjectId id)
        {
            List<Alert> alerts = await repo.GetAlertsByField("_id", id);
            return alerts.Count == 1 ? alerts[0] : null;
        }

        public static Task<List<ObjectId>> GetExistingAlertIds(string[] alertIds)
        {
            return repo.GetExistingAlertIds(alertIds);
        }

        public static Task<List<Alert>> GetAlertsByUserId(ObjectId userId)
        {
            return repo.GetAlertsByUserId(userId);
        }

        public static async Task SendAlert(Alert alert, bool shouldMail, string? senderId)
        {
            if (senderId != null) // can be null if the alert is because someone is trying to create an account
            {
                alert.targetRecipients.Remove(senderId);
            }

            alert.remainingRecipients = alert.targetRecipients;

            if (alert.targetRecipients.Count < 1)
            {
                // Don't create an alert if there's no one to recieve it(It'll crash)
                return;
            }

            await CreateAlert(alert);

            if (shouldMail)
            {
                var mailAddresses = await UserService.GetUserEmailAddresses(alert.targetRecipients);
                string mailBody = alert.link == null ? alert.body : $"{alert.body}\n\nראו עוד בקישור הבא:\n{alert.link}";
                await EmailService.SendMailToAddresses(mailAddresses.ToArray(), alert.title, mailBody);
            }
        }

        public static Task CreateAlert(Alert alert)
        {
            return repo.CreateAlert(alert);
        }

        public async static Task<UpdateResult> MarkAsViewed(string alertId, string userId)
        {
            var alertObjectId = ObjectId.Parse(alertId);
            Alert? existingAlert = await GetAlertById(alertObjectId);

            if (existingAlert == null)
            {
                throw new KeyNotFoundException($"No alert with id {alertId} exists");
            }

            List<string> remainingAlertees = existingAlert.remainingRecipients;
            remainingAlertees.Remove(userId);

            return await repo.ViewAlert(alertObjectId, remainingAlertees);
        }

        public async static Task<Alert> FillAlertByAssociations(Alert input, string[] associationIds)
        {
            var usersInAssociations = await AssociationService.GetAllUsersInAssociations(associationIds);

            return FillAlert(input, usersInAssociations);
        }

        public async static Task<Alert> FillAlertBySchool(Alert input, ObjectId? schoolId)
        {
            if (schoolId == null || schoolId == ObjectId.Empty)
            {
                return input;
            }

            var usersInSchools = await UserService.GetUsersBySchool(schoolId.Value);

            return FillAlert(input, usersInSchools);
        }

        public async static Task<Alert> FillAlertByRoles(Alert input, string[] roles)
        {
            var usersWithRole = await UserService.GetUsersByRoles(roles);

            if (usersWithRole.Count < 1)
            {
                return input;
            }

            return FillAlert(input, usersWithRole);
        }

        public static Alert FillAlert(Alert input, IEnumerable<User> users)
        {
            var userIds = users.Map(user => user.id);
            input.targetRecipients = userIds;
            input.remainingRecipients = userIds;

            return input;
        }

        public static Task<bool> DeleteAlert(string alertId)
        {
            return repo.DeleteAlert(alertId);
        }
    }

}
