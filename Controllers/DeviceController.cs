using Microsoft.AspNetCore.Mvc;
using IstiklalLorawanAPI.Services;
using System.Threading.Tasks;

namespace IstiklalLorawanAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public DeviceController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpPost("toggle-recording")]
        public async Task<IActionResult> ToggleRecording([FromBody] ToggleRecordingRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.DevEui))
            {
                return BadRequest(new { status = "error", message = "Invalid request" });
            }

            await _firebaseService.SetRecordingAsync(request.DevEui, request.Enabled);
            return Ok(new { status = "success" });
        }
    }

    public class ToggleRecordingRequest
    {
        public string DevEui { get; set; }
        public bool Enabled { get; set; }
    }
}
