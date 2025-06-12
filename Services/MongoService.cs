using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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

        public async static Task<List<TValue>> GetExistingValues<TValue>(IMongoCollection<BsonDocument> collection, string field, TValue[] values, Func<BsonDocument, TValue> deserializer)
        {
            var filter = Builders<BsonDocument>.Filter.In(field, values);
            var projection = Builders<BsonDocument>.Projection.Include(field);

            var documents = await collection.Find(filter).Project(projection).ToListAsync();
            var existingValues = documents.Select(deserializer).ToList();

            return existingValues;
        }

        public async static Task<bool> DoesEntityWithFieldExist<TValue>(IMongoCollection<BsonDocument> collection, string field, TValue value)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(field, value);
            long count = await collection.CountDocumentsAsync(filter);

            return count > 0;
        }

        public static FilterDefinition<BsonDocument> GetFilterEq<TValue>(string field, TValue value)
        {
            return Builders<BsonDocument>.Filter.Eq(field, value);
        }

        public static FilterDefinition<BsonDocument> MergeFilterDefinitions(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            if (filterList == null || filterList.Count() == 0)
            {
                return Builders<BsonDocument>.Filter.Empty;
            }

            return Builders<BsonDocument>.Filter.And(filterList);
        }
        public static Task<List<TEntity>> GetEntitiesByMultipleFilters<TEntity>(IMongoCollection<BsonDocument> collection, IEnumerable<FilterDefinition<BsonDocument>> filterList, Func<BsonDocument, TEntity> deserializer) where TEntity : MongoEntity {
            FilterDefinition<BsonDocument> combinedFilter = MergeFilterDefinitions(filterList);

            return GetEntitiesByFilter(collection, combinedFilter, deserializer);
        }
        public async static Task<List<TEntity>> GetEntitiesByFilter<TEntity>(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, Func<BsonDocument, TEntity> deserializer) where TEntity: MongoEntity
        {
            List<BsonDocument> filteredEntities = await collection.Find(filter).ToListAsync();

            return filteredEntities.Select(deserializer).ToList();
        }
        public static Task<List<TEntity>> GetEntitiesByField<TEntity, TValue>(IMongoCollection<BsonDocument> collection, string field, TValue value, Func<BsonDocument, TEntity> deserializer) where TEntity: MongoEntity
        {
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq(field, value);

            return GetEntitiesByFilter(collection, filter, deserializer);
        }

        public async static Task<List<TEntity>> GetEntitiesByFieldValueIn<TEntity, TValue>(IMongoCollection<BsonDocument> collection, string field, TValue[] values, Func<BsonDocument, TEntity> deserializer) where TEntity : MongoEntity
        {
            var filter = Builders<BsonDocument>.Filter.In(field, values);
            var filteredEntites = await collection.Find(filter).ToListAsync();

            return filteredEntites.Select(deserializer).ToList();
        }

        public async static Task CreateEntity<TEntity>(IMongoCollection<BsonDocument> collection, TEntity entityToCreate) where TEntity : MongoEntity
        {
            var createdBson = entityToCreate.ToBsonDocument();
            await collection.InsertOneAsync(createdBson);
        }
        public async static Task<ReplaceOneResult> UpsertEntity<TEntity>(IMongoCollection<BsonDocument> collection, TEntity upsertedEntity) where TEntity : MongoEntity
        {
            if (!upsertedEntity.id.IsObjectId())
            {
                await CreateEntity(collection, upsertedEntity);
                return new ReplaceOneResult.Acknowledged(0, 1, null);
            }

            var upsertedEntityId = ObjectId.Parse(upsertedEntity.id);
            BsonDocument[] idFilter = { new BsonDocument("$match", new BsonDocument("_id", upsertedEntityId)) };
            BsonDocument[] fullAggregatePipeLine = Utils.Merge(idFilter, idConverterPipeline);

            var entitiesToUpsert = await collection.Aggregate<BsonDocument>(fullAggregatePipeLine).ToListAsync();

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

        public async static Task<UpdateResult> UpdateEntitiesByField<TEntity, TFieldValue, TNewValue>(
            IMongoCollection<BsonDocument> collection, string fieldToCheck, TFieldValue valueToCheck,
            string fieldToUpdate, TNewValue updateValue, Func<BsonDocument, TEntity> deserializer) where TEntity : MongoEntity
        {
            var filter = Builders<BsonDocument>.Filter.Eq(fieldToCheck, valueToCheck);
            var update = Builders<BsonDocument>.Update.Set(fieldToUpdate, updateValue);

            return await collection.UpdateManyAsync(filter, update);
        }


        public async static Task<bool> DeleteEntity(IMongoCollection<BsonDocument> collection, string entityId)
        {
            if (!entityId.IsObjectId())
            {
                return false;
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(entityId));
            await collection.DeleteOneAsync(filter);

            return true;
        }
    }
}