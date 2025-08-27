using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Super_Sonic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BadrController : ControllerBase
    {

        [HttpGet("Badr")]
        public IActionResult GetBadr()
        {
            return Ok("Badr is ready to be serve 🥰🥰🥰🥰🥰");
        }

        [HttpGet("Mostafa")]
        public IActionResult GetMostafa()
        {
            return Ok("Badr is ready to be serve 🥰🥰🥰🥰🥰");
        }

                [HttpGet("AutoTest")]
        public IActionResult AutoTestedAndDeployedAction()
        {
            return Ok("No one deployed meeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee ");
        }

       [HttpGet("Zawy")]
        public IActionResult PrintZawy()
        {
            return Ok("Hello this is Ahmed Abd Elkarem");
        }
    }
}
