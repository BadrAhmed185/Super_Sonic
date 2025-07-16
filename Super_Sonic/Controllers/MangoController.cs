using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Super_Sonic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MangoController : ControllerBase
    {

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Mango is ready to be served!");
        }
    }
}
