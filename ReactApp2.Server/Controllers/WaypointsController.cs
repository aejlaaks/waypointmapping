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
        private readonly IWaypointService _waypointService;
        private readonly ILogger<WaypointsController> _logger;

        public WaypointsController(IWaypointService waypointService, ILogger<WaypointsController> logger)
        {
            _waypointService = waypointService;
            _logger = logger;
        }

        [HttpPost("generatePoints")]
        public async Task<IActionResult> GeneratePoints([FromBody] GeneratePointsRequestDTO request)
        {
            try
            {
                _logger.LogInformation("Generating waypoints with {BoundsType}", request.BoundsType);
                
                string action = request.AllPointsAction ?? request.Action;
                double lineSpacing = request.LineSpacing > 0 ? request.LineSpacing : request.Distance;
                int photoInterval = request.PhotoInterval > 0 ? request.PhotoInterval : (int)request.Interval;
                bool isNorthSouth = request.IsNorthSouth || request.isNorthSouth;
                
                // If we are using the legacy implementation
                var result = await _waypointService.GenerateWaypointsAsync(
                    action,
                    request.UnitType,
                    request.Altitude,
                    request.Speed,
                    0, // Angle is no longer used
                    lineSpacing,
                    request.Bounds,
                    request.BoundsType,
                    request.StartingIndex,
                    photoInterval,
                    request.UseEndpointsOnly,
                    isNorthSouth);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating waypoints");
                return StatusCode(500, new { error = "Error generating waypoints", message = ex.Message });
            }
        }

        [HttpPost("generatePointsV2")]
        public async Task<IActionResult> GeneratePointsV2([FromBody] GeneratePointsRequestDTO request)
        {
            try
            {
                _logger.LogInformation("Generating waypoints with V2 endpoint for {BoundsType}", request.BoundsType);
                
                string action = request.AllPointsAction ?? request.Action;
                double lineSpacing = request.LineSpacing > 0 ? request.LineSpacing : request.Distance;
                int photoInterval = request.PhotoInterval > 0 ? request.PhotoInterval : (int)request.Interval;
                bool isNorthSouth = request.IsNorthSouth || request.isNorthSouth;
                
                // Create parameter object for new implementation
                var parameters = new WaypointParameters
                {
                    Action = action,
                    UnitType = request.UnitType,
                    Altitude = request.Altitude,
                    Speed = request.Speed,
                    LineSpacing = lineSpacing,
                    StartingIndex = request.StartingIndex,
                    PhotoInterval = photoInterval,
                    UseEndpointsOnly = request.UseEndpointsOnly,
                    IsNorthSouth = isNorthSouth
                };

                // Map the boundaries to shape data
                var shapes = new List<ShapeData>();
                
                if (request.Bounds != null && request.Bounds.Count > 0)
                {
                    switch (request.BoundsType?.ToLower())
                    {
                        case "rectangle":
                            shapes.Add(new ShapeData
                            {
                                Id = "1",
                                Type = ShapeTypes.Rectangle,
                                Coordinates = request.Bounds
                            });
                            break;
                        
                        case "polygon":
                            shapes.Add(new ShapeData
                            {
                                Id = "1",
                                Type = ShapeTypes.Polygon,
                                Coordinates = request.Bounds
                            });
                            break;
                        
                        case "circle":
                            // For circle, get center point and radius
                            var center = request.Bounds[0];
                            double radius = center.Radius > 0 ? center.Radius : 100; // Default to 100m if not specified
                            
                            shapes.Add(new ShapeData
                            {
                                Id = "1",
                                Type = ShapeTypes.Circle,
                                Coordinates = new List<Coordinate> { center },
                                Radius = radius
                            });
                            break;
                        
                        case "polyline":
                            shapes.Add(new ShapeData
                            {
                                Id = "1",
                                Type = ShapeTypes.Polyline,
                                Coordinates = request.Bounds
                            });
                            break;
                    }
                }
                
                // Use the new interface with shapes and parameters
                var result = await _waypointService.GenerateWaypointsAsync(shapes, parameters);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating waypoints with V2 endpoint");
                return StatusCode(500, new { error = "Error generating waypoints", message = ex.Message });
            }
        }
    }
}
