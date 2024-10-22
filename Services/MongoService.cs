using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;

namespace teachers_lounge_server.Services
{
    public class MongoService
    {
        private const string DEFAULT_DB = "mahat";
        private static string username = Environment.GetEnvironmentVariable("MONGO_USERNAME") ?? "";
        private static string password = Environment.GetEnvironmentVariable("MONGO_PASSWORD") ?? "";
        private static string connectionUri = $"mongodb+srv://{username}:{password}@freecluster.lbwfi.mongodb.net/?retryWrites=true&w=majority&appName=FreeCluster";
        public static string[] Details
        {
            get
            {
                return new string[] { username, password, connectionUri };
            }
        }
        public static IMongoCollection<T> GetCollection<T>(string colelctionName)
        {
            return GetCollection<T>(DEFAULT_DB, colelctionName);
        }
        public static IMongoCollection<T> GetCollection<T>(string databaseName, string collectionName)
        {
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionUri);

            // Set the ServerApi field of the settings object to set the version of the Stable API on the client
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            // Create a new client and connect to the server
            MongoClient client = new MongoClient(settings);

            return client.GetDatabase(databaseName).GetCollection<T>(collectionName);
        }
        private static BsonDocument[] idConverterPipeline = {
                new BsonDocument("$set", new BsonDocument("id", "$_id")),
                new BsonDocument("$unset", "_id"),
            };
        public async static Task<List<T>> GetEntireCollection<T>(IMongoCollection<BsonDocument> collection)
        {
            return await collection.Aggregate<T>(idConverterPipeline).ToListAsync();
        }
        public async static Task CreateEntity<T>(IMongoCollection<BsonDocument> collection, T entityToCreate) where T : MongoEntity
        {
            var createdBson = entityToCreate.ToBsonDocument();
            await collection.InsertOneAsync(createdBson);
        }
        public async static Task<ReplaceOneResult> UpsertEntity<T>(IMongoCollection<BsonDocument> collection, T upsertedEntity) where T : MongoEntity
        {
            if (upsertedEntity.id.Length != 24)
            {
                // Not even a real id so no reason to check if it exists
                await CreateEntity(collection, upsertedEntity);
                return new ReplaceOneResult.Acknowledged(0, 1, null);
            }

            var upsertedEntityId = ObjectId.Parse(upsertedEntity.id);
            BsonDocument[] idFilter = { new BsonDocument("$match", new BsonDocument("_id", upsertedEntityId)) };
            BsonDocument[] fullAggregatePipeLine = Utils.Merge(idFilter, idConverterPipeline);

            var entitiesToUpsert = await collection.Aggregate<T>(fullAggregatePipeLine).ToListAsync();

            if (entitiesToUpsert.Count > 1)
            {
                throw new Exception("Upsert affected multiple rows");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", upsertedEntityId);
            var upsertedBson = upsertedEntity.ToBsonDocument();
            upsertedBson.Remove("_id");

            var upsertResult = await collection.ReplaceOneAsync(filter, upsertedBson, new ReplaceOptions { IsUpsert = true });

            return upsertResult;
        }
    }
}