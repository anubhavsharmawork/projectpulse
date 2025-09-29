using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Root() => Ok(new { status = "ok" });
    }
}
