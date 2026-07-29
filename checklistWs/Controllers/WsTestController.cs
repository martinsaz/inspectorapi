using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WsTestController : ControllerBase
    {
        [HttpGet]
        public IActionResult HolaMundo()
        {
            return Ok("Hola Mundo");
        }
    }
}
