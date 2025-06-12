using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("associations")]
    public class AssociationController : ControllerBase
    {
        private readonly ILogger<AssociationController> _logger;

        public AssociationController(ILogger<AssociationController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "All associations")]
        [UserIdValidator]
        public async Task<ActionResult<IEnumerable<Association>>> GetAllAssociations()
        {
            return Ok(await AssociationService.GetAllAssociations());
        }

        [HttpGet("type/{typeName}/from/{schoolId}", Name = "All associations of given type")]
        [UserIdValidator]
        public async Task<ActionResult<IEnumerable<Association>>> GetAllAssociationsOfType(string typeName, string schoolId)
        {
            return Ok(await AssociationService.GetAssociationsByTypenameAndSchool(typeName, schoolId));
        }

        [HttpPost(Name = "Upsert association")]
        public async Task<ActionResult<ReplaceOneResult>> UpsertAssociation([FromBody] Association association)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            // If it's an updated association, a user should be able to update an association to not be tied to his school
            // leading to a situation where if you check by the association fromBody he won't be authorized because the association is no longer linked to the school
            bool isAuthorized = association.id == null
                ? await AssociationService.CanUserAffectAssociation(userId, association)
                : await AssociationService.CanUserAffectAssociation(userId, association.id);
            if (!isAuthorized)
            {
                return Unauthorized($"You do not have permissions to upsert the association {association.name}");
            }

            return Ok(await AssociationService.UpsertAssociation(userId!, association));
        }

        [HttpDelete("{associationId}", Name = "Delete association")]
        public async Task<ActionResult<bool>> DeleteAssociation(string associationId)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!await AssociationService.CanUserAffectAssociation(userId, associationId))
            {
                return Unauthorized($"You do not have permissions to delete the association {associationId}");
            }

            return Ok(await AssociationService.DeleteAssociation(associationId));
        }
    }
}
