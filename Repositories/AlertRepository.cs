using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class AlertRepository : Repository
    {
        protected override string CollectionName => "alerts";

        public Task<List<Alert>> GetAllAlerts()
        {
            return MongoService.GetEntireCollection<Alert>(Collection);
        }

        public Task<List<Alert>> GetAlertsByField<TValue>(string field, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, field, value, Alert.FromBsonDocument);
        }

        public Task<List<ObjectId>> GetExistingAlertIds(string[] alertIds)
        {
            ObjectId[] validIds = alertIds.FilterAndMap(id => id.IsObjectId(), id => ObjectId.Parse(id));

            return MongoService.GetExistingValues(Collection, "_id", validIds, doc => doc.GetValue("_id").AsObjectId);
        }

        public Task<List<Alert>> GetAlertsByUserId(ObjectId userId)
        {
            return MongoService.GetEntitiesByFieldContainsValue(Collection, "targetUsers", userId, Alert.FromBsonDocument);
        }

        public Task CreateAlert(Alert alert)
        {
            return MongoService.CreateEntity(Collection, alert);
        }

        public Task<UpdateResult> ViewAlert(ObjectId alertId, List<string> remainingRecipients)
        {
            return MongoService.UpdateEntitiesByField(Collection, "_id", alertId, "remainingRecipients", remainingRecipients, Alert.FromBsonDocument);
        }

        public Task<bool> DeleteAlert(string alertId)
        {
            return MongoService.DeleteEntity(Collection, alertId);
        }
    }

}
