using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Repositories
{
    public class AssociationRepository: Repository
    {
        protected override string CollectionName => "associations";

        public Task<List<Association>> GetAllAssociations()
        {
            return MongoService.GetEntireCollection<Association>(Collection);
        }
        public Task<List<Association>> GetAssocationsByField<TValue>(string fieldName, TValue value)
        {
            return MongoService.GetEntitiesByField(Collection, fieldName, value, Association.FromBsonDocument);
        }
        public Task<List<Association>> GetAssociationsByMultipleFilters(IEnumerable<FilterDefinition<BsonDocument>> filterList)
        {
            return MongoService.GetEntitiesByMultipleFilters(Collection, filterList, Association.FromBsonDocument);
        }

        public Task<ReplaceOneResult> UpsertAssociation(Association association)
        {
            return MongoService.UpsertEntity(Collection, association);
        }

        public Task<bool> DeleteAssociation(string associationId)
        {
            return MongoService.DeleteEntity(Collection, associationId);
        }
    }
}
