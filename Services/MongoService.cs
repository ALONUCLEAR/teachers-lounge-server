using MongoDB.Bson;
using MongoDB.Driver;

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
        public static IMongoCollection<T> getCollection<T>(string colelctionName)
        {
            return getCollection<T>(DEFAULT_DB, colelctionName);
        }
        public static IMongoCollection<T> getCollection<T>(string databaseName, string collectionName)
        {
            MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionUri);

            // Set the ServerApi field of the settings object to set the version of the Stable API on the client
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            // Create a new client and connect to the server
            MongoClient client = new MongoClient(settings);

            return client.GetDatabase(databaseName).GetCollection<T>(collectionName);
        }
        public async static Task<List<T>> getEntireCollection<T>(string collectionName)
        {
            return await getEntireCollection<T>(DEFAULT_DB, collectionName);
        }

        public async static Task<List<T>> getEntireCollection<T>(string database, string collectionName)
        {
            var collection = getCollection<BsonDocument>(database, collectionName);
            var pipeline = new[]
            {
                new BsonDocument("$set", new BsonDocument("id", "$_id")),
                new BsonDocument("$unset", "_id"),
            };

            return await collection.Aggregate<T>(pipeline).ToListAsync();
        }
    }
}
