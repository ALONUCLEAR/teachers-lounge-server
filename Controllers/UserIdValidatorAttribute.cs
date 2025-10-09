using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace teachers_lounge_server.Controllers
{
    public class UserIdValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.ContainsKey("userId"))
            {
                context.Result = new BadRequestObjectResult("Missing 'userId' header.");
            }

            base.OnActionExecuting(context);
        }
    }
}
