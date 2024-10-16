using KarttaBackEnd2.Server.DTOs;
using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KarttaBackEnd2.Server.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WaypointsController : ControllerBase
    {
        private readonly WaypointService _waypointService;

        public WaypointsController( WaypointService waypointService)
        {
            _waypointService = waypointService;
        }

        [HttpPost("generatePoints")]

        public async Task<IActionResult> GeneratePoints([FromBody] GeneratePointsRequestDTO request)
        {
                var result = await _waypointService.GenerateWaypointsAsync(request.AllPointsAction,
                    request.UnitType,
                    request.Altitude,
                    request.Speed,
                    request.Angle,
                    request.In_Distance,
                    request.Bounds,
                    request.BoundsType,
                    request.StartingIndex,
                    request.Interval,
                    request.useEndpointsOnly,
                    request.isNorthSouth);

                return Ok(result);
            
        }
    }
}
