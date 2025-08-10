using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using teachers_lounge_server.Entities;
using teachers_lounge_server.Services;

namespace teachers_lounge_server.Controllers
{
    [ApiController]
    [Route("schools")]
    public class SchoolController : ControllerBase
    {
        private readonly ILogger<SchoolController> _logger;

        public SchoolController(ILogger<SchoolController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "All schools")]
        public async Task<ActionResult<IEnumerable<School>>> GetAllSchools()
        {
            return Ok(await SchoolService.GetAllSchools());
        }

        [HttpGet("{id}", Name = "Get School By Id")]
        public async Task<ActionResult<School>> GetSchoolById(string id)
        {
            if (!id.IsObjectId())
            {
                return BadRequest($"Id {id} doesn't fit the object Id format");
            }

            School? school = await SchoolService.GetSchoolById(ObjectId.Parse(id));
            return school == null ? NoContent() : Ok(school);
        }

        [HttpPost("upsert", Name = "Upsert school")]
        public async Task<ActionResult<ReplaceOneResult>> UpsertSchool([FromBody] School school)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!await UserService.HasPermissions(userId, Role.SuperAdmin))
            {
                return Unauthorized($"You do not have permissions to upsert the school {school.name}");
            }

            return Ok(await SchoolService.UpsertSchool(school));
        }

        [HttpDelete("{schoolId}", Name = "Delete school")]
        public async Task<ActionResult<bool>> DeleteSchool(string schoolId)
        {
            if (!Request.Headers.TryGetValue("userId", out var userId))
            {
                return BadRequest("userId header is missing");
            }

            if (!await UserService.HasPermissions(userId, Role.SuperAdmin))
            {
                return Unauthorized($"You do not have permissions to delete the school {schoolId}");
            }

            return Ok(await SchoolService.DeleteSchool(schoolId));
        }
    }
}
