using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace istiklal_karacasu_lorawan.Controllers
{

    [ApiController]
    [Route("api/healt_check")]
    public class Healt_CheckController : ControllerBase
    {
        // GET: api/<Healt_CheckController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Success" };
        }       
    }
}
