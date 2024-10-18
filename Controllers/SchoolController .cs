using Microsoft.AspNetCore.Mvc;
using teachers_lounge_server.Entities;

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

        private static School[] MockSchoools
        {
            get
            {
                var city1 = new GovernmentData(1, "בת ים", 6200);
                var city2 = new GovernmentData(2, "אשדוד", 300);
                var street1 = new Street(3, "הרצל", 68, 52);
                var street2 = new Street(4, "אילת", 69, 123);

                string[] schoolNames = ["ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי"];
                School[] schools = new School[schoolNames.Length];
                Random rand = new Random((int)DateTime.Now.ToOADate());

                for (int index = 0; index < schools.Length; index++)
                {
                    var city = rand.Next(2) > 0 ? new GovernmentData(city1) : new GovernmentData(city2);
                    var street = rand.Next(2) > 0 ? new Street(street1) : new Street(street2);
                    int houseNum = rand.Next(100) + 1;
                    schools[index] = new School($"{index + 1}", schoolNames[index], city, new Address(street, houseNum));
                }

                return schools;
            }
        }
        // TODO: connect to mongo

        [HttpGet(Name = "GetAllSchools")]
        public ActionResult<IEnumerable<School>> Get()
        {
            return Ok(MockSchoools);
        }
    }
}
