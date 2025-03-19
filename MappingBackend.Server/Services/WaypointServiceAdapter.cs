using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Adapter service to translate between the old and new waypoint service implementations
    /// This will allow for a smooth transition from the old API to the new one
    /// </summary>
    public class WaypointServiceAdapter : IWaypointService
    {
        private readonly WaypointServiceV2 _newWaypointService;
        private readonly WaypointService _legacyWaypointService;
        private readonly ILogger<WaypointServiceAdapter> _logger;
        private readonly bool _useNewImplementation;

        public WaypointServiceAdapter(
            WaypointServiceV2 newWaypointService,
            WaypointService legacyWaypointService,
            ILogger<WaypointServiceAdapter> logger)
        {
            _newWaypointService = newWaypointService;
            _legacyWaypointService = legacyWaypointService;
            _logger = logger;
            
            // Set this to false during initial transition to use the legacy method as fallback
            // Set to true when confident the new implementation works correctly
            _useNewImplementation = true;
        }

        /// <summary>
        /// Adapter method for the old interface
        /// </summary>
        public async Task<List<Waypoint>> GenerateWaypointsAsync(
            string action,
            int unitType_in,
            double altitude,
            double speed,
            int angle, // This parameter is kept for backward compatibility but not used
            double in_distance,
            List<Coordinate> bounds,
            string boundsType,
            int startingIndex,
            double photoInterval,
            bool useEndpointsOnly,
            bool isNorthSouth)
        {
            _logger.LogInformation("Adapting from old to new waypoint service implementation");
            
            try
            {
                if (!_useNewImplementation)
                {
                    // During transition, we can toggle between implementations
                    return await _legacyWaypointService.GenerateWaypointsAsync(
                        action, unitType_in, altitude, speed, angle, in_distance,
                        bounds, boundsType, startingIndex, photoInterval,
                        useEndpointsOnly, isNorthSouth);
                }

                // Create parameter object for new implementation
                var parameters = new WaypointParameters
                {
                    Action = action,
                    UnitType = unitType_in,
                    Altitude = altitude,
                    Speed = speed,
                    LineSpacing = in_distance,
                    StartingIndex = startingIndex,
                    PhotoInterval = photoInterval,
                    UseEndpointsOnly = useEndpointsOnly,
                    IsNorthSouth = isNorthSouth
                };

                // Map the old bounds to shape data
                var shapes = new List<ShapeData>();
                
                switch (boundsType?.ToLower())
                {
                    case "rectangle":
                        shapes.Add(new ShapeData
                        {
                            Id = "1",
                            Type = ShapeTypes.Rectangle,
                            Coordinates = bounds
                        });
                        break;
                    
                    case "polygon":
                        shapes.Add(new ShapeData
                        {
                            Id = "1",
                            Type = ShapeTypes.Polygon,
                            Coordinates = bounds
                        });
                        break;
                    
                    case "circle":
                        // For circle, get center point and radius
                        if (bounds != null && bounds.Count > 0)
                        {
                            var center = bounds[0];
                            double radius = center.Radius > 0 ? center.Radius : 100; // Default to 100m if not specified
                            
                            shapes.Add(new ShapeData
                            {
                                Id = "1",
                                Type = ShapeTypes.Circle,
                                Coordinates = new List<Coordinate> { center },
                                Radius = radius
                            });
                        }
                        break;
                    
                    case "polyline":
                        shapes.Add(new ShapeData
                        {
                            Id = "1",
                            Type = ShapeTypes.Polyline,
                            Coordinates = bounds
                        });
                        break;
                    
                    default:
                        _logger.LogWarning("Unknown bounds type: {BoundsType}", boundsType);
                        break;
                }
                
                // Call the new implementation
                var result = await _newWaypointService.GenerateWaypointsAsync(shapes, parameters);
                
                if (result == null || result.Count == 0)
                {
                    _logger.LogWarning("New implementation returned no waypoints for bounds type: {BoundsType}", boundsType);
                    // Try legacy implementation as fallback
                    _logger.LogInformation("Falling back to legacy implementation for empty results");
                    return await _legacyWaypointService.GenerateWaypointsAsync(
                        action, unitType_in, altitude, speed, angle, in_distance,
                        bounds, boundsType, startingIndex, photoInterval,
                        useEndpointsOnly, isNorthSouth);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in new implementation, falling back to legacy implementation");
                
                // Fallback to legacy implementation if the new one fails
                return await _legacyWaypointService.GenerateWaypointsAsync(
                    action, unitType_in, altitude, speed, angle, in_distance,
                    bounds, boundsType, startingIndex, photoInterval,
                    useEndpointsOnly, isNorthSouth);
            }
        }

        /// <summary>
        /// New implementation method that will be used directly in the future
        /// </summary>
        public async Task<List<Waypoint>> GenerateWaypointsAsync(List<ShapeData> shapes, WaypointParameters parameters)
        {
            try
            {
                var result = await _newWaypointService.GenerateWaypointsAsync(shapes, parameters);
                
                if (result == null || result.Count == 0)
                {
                    _logger.LogWarning("New implementation returned no waypoints when called directly");
                    
                    if (shapes == null || shapes.Count == 0 || shapes[0].Coordinates == null)
                    {
                        _logger.LogError("Cannot fall back to legacy implementation: invalid shapes data");
                        return new List<Waypoint>(); // Return empty list rather than null
                    }
                    
                    // Get the first shape - legacy only supports one shape
                    var shape = shapes[0];
                    
                    _logger.LogInformation("Falling back to legacy implementation for empty results");
                    // Try legacy implementation as fallback
                    return await _legacyWaypointService.GenerateWaypointsAsync(
                        parameters.Action,
                        parameters.UnitType,
                        parameters.Altitude,
                        parameters.Speed,
                        0, // Angle is not used
                        parameters.LineSpacing,
                        shape.Coordinates,
                        shape.Type,
                        parameters.StartingIndex,
                        parameters.PhotoInterval,
                        parameters.UseEndpointsOnly,
                        parameters.IsNorthSouth
                    );
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in new implementation, attempting to map to legacy implementation");
                
                if (shapes == null || shapes.Count == 0 || shapes[0].Coordinates == null)
                {
                    throw; // Can't map to legacy without shapes
                }
                
                // Get the first shape - legacy only supports one shape
                var shape = shapes[0];
                
                // Call the legacy method with mapped parameters
                return await _legacyWaypointService.GenerateWaypointsAsync(
                    parameters.Action,
                    parameters.UnitType,
                    parameters.Altitude,
                    parameters.Speed,
                    0, // Angle is not used
                    parameters.LineSpacing,
                    shape.Coordinates,
                    shape.Type,
                    parameters.StartingIndex,
                    parameters.PhotoInterval,
                    parameters.UseEndpointsOnly,
                    parameters.IsNorthSouth
                );
            }
        }
    }
} 