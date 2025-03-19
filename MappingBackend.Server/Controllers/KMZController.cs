using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace KarttaBackEnd2.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KMZController : ControllerBase
    {
        private readonly 
            IKMZService _kmzService;

        public KMZController(IKMZService kmzService)
        {
            _kmzService = kmzService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateKmlAndWpml([FromBody] FlyToWaylineRequest request)
        {
            if (request == null)
            {
                return BadRequest("The request field is required.");
            }
            // Generate KMZ file asynchronously with service
            byte[] kmzFile = await _kmzService.GenerateKmzAsync(request);

            // Return KMZ file for download
            return File(kmzFile, "application/vnd.google-earth.kmz", "output.kmz");
        }
    }
}
