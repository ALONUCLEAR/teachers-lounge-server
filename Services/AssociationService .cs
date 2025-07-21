using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Repositories;

namespace teachers_lounge_server.Services
{
    public class AssociationService
    {
        private static AssociationRepository repo => new AssociationRepository();

        public static Task<List<Association>> GetAllAssociations()
        {
            return repo.GetAllAssociations();
        }

        public async static Task<List<Association>> GetAssociationsByTypenameAndSchool(string typename, string schoolId)
        {
            if (!AssociationType.isValid(typename))
            {
                return new List<Association>();
            }

            var filterList = new List<FilterDefinition<BsonDocument>>();
            // If both the field and values are arrays, In also acts like contains
            filterList.Add(Builders<BsonDocument>.Filter.In("associatedSchools", new ObjectId[] { ObjectId.Parse(schoolId) }));
            filterList.Add(Builders<BsonDocument>.Filter.Eq("type", typename));

            return await repo.GetAssociationsByMultipleFilters(filterList);
        }

        public async static Task<bool> IsAssociationValid(Association association)
        {
            var filterList = new List<FilterDefinition<BsonDocument>>();
            filterList.Add(Builders<BsonDocument>.Filter.In("associatedSchools", association.associatedSchools.Map(ObjectId.Parse)));
            filterList.Add(Builders<BsonDocument>.Filter.Eq("type", association.type));
            filterList.Add(Builders<BsonDocument>.Filter.Eq("name", association.name));

            if (association.id.IsObjectId())
            {
                filterList.Add(Builders<BsonDocument>.Filter.Ne("_id", ObjectId.Parse(association.id)));
            }

            var associations = await repo.GetAssociationsByMultipleFilters(filterList);

            return associations.Count == 0;
        }

        public static async Task<bool> CanUserAffectAssociation(string? userId, Association association)
        {
            var user = await UserService.GetUserById(userId);

            if (user == null)
            {
                return false;
            }

            bool isSupport = await UserService.HasPermissions(userId, Role.Support);

            if (isSupport)
            {
                // support users don't necessarily have the school associated with them
                return true;
            }

            bool doShareSchool = user.associatedSchools.Some(schoolId => association.associatedSchools.Contains(schoolId));

            return await UserService.HasPermissions(userId, Role.Admin) && doShareSchool;
        }

        public static async Task<bool> CanUserAffectAssociation(string? userId, string associationId)
        {
            var associationsWithId = await repo.GetAssocationsByField("_id", ObjectId.Parse(associationId));

            if (associationsWithId.Count != 1)
            {
                return false;
            }

            return await CanUserAffectAssociation(userId, associationsWithId[0]);
        }

        public static async Task<ReplaceOneResult> UpsertAssociation(string userId, Association association)
        {
            return await repo.UpsertAssociation(association);
        }

        public static Task<bool> DeleteAssociation(string associationId)
        {
            return repo.DeleteAssociation(associationId);
        }
    }
}
