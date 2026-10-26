using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Permissions = VirtoCommerce.AdminAngularJs.Core.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.AdminAngularJs.Web.Controllers.Api;

[Authorize]
[Route("api/admin-angular-js")]
public class AdminAngularJsController : Controller
{
    // GET: api/admin-angular-js
    /// <summary>
    /// Get message
    /// </summary>
    /// <remarks>Return "Hello world!" message</remarks>
    [HttpGet]
    [Route("")]
    [Authorize(Permissions.Read)]
    public ActionResult<string> Get()
    {
        return Ok(new { result = "Hello world!" });
    }
}
