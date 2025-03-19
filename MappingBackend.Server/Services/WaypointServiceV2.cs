using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Updated implementation of IWaypointService that delegates shape-specific processing to specialized services
    /// </summary>
    public class WaypointServiceV2 : IWaypointService
    {
        private readonly IEnumerable<IShapeService> _shapeServices;
        private readonly ILogger<WaypointServiceV2> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaypointServiceV2"/> class.
        /// </summary>
        /// <param name="shapeServices">Collection of shape services</param>
        /// <param name="logger">Logger for the waypoint service</param>
        public WaypointServiceV2(IEnumerable<IShapeService> shapeServices, ILogger<WaypointServiceV2> logger)
        {
            _shapeServices = shapeServices;
            _logger = logger;
        }

        /// <summary>
        /// Legacy method implementation - delegates to the new method
        /// </summary>
        public async Task<List<Waypoint>> GenerateWaypointsAsync(
            string action,
            int unitType_in,
            double altitude,
            double speed,
            int angle, // Parameter is kept for backward compatibility but not used
            double in_distance,
            List<Coordinate> bounds,
            string boundsType,
            int startingIndex,
            double photoInterval,
            bool useEndpointsOnly,
            bool isNorthSouth)
        {
            _logger.LogInformation("Legacy GenerateWaypointsAsync method called, converting to new format");
            
            // Create parameter object for new implementation
            var parameters = new WaypointParameters
            {
                Action = action,
                UnitType = unitType_in,
                Altitude = altitude,
                Speed = speed,
                LineSpacing = in_distance,
                StartingIndex = startingIndex,
                PhotoInterval = (int)photoInterval,
                UseEndpointsOnly = useEndpointsOnly,
                IsNorthSouth = isNorthSouth
            };

            // Map the old bounds to shape data
            var shapes = new List<ShapeData>();
            
            // Create a shape based on the bounds type
            if (bounds != null && bounds.Count > 0)
            {
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
                        var center = bounds[0];
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
                            Coordinates = bounds
                        });
                        break;
                    
                    default:
                        _logger.LogWarning("Unknown bounds type: {BoundsType}", boundsType);
                        break;
                }
            }
            
            // Use the new implementation with the converted parameters
            return await GenerateWaypointsAsync(shapes, parameters);
        }

        /// <inheritdoc />
        public async Task<List<Waypoint>> GenerateWaypointsAsync(List<ShapeData> shapes, WaypointParameters parameters)
        {
            _logger.LogInformation("Generating waypoints for {ShapeCount} shapes", shapes?.Count ?? 0);
            
            if (shapes == null || shapes.Count == 0)
            {
                _logger.LogWarning("No shapes provided for waypoint generation");
                return new List<Waypoint>();
            }
            
            var allWaypoints = new List<Waypoint>();
            int currentIndex = parameters.StartingIndex;
            
            foreach (var shape in shapes)
            {
                try
                {
                    var service = _shapeServices.FirstOrDefault(s => s.CanHandleShapeType(shape.Type));
                    
                    if (service == null)
                    {
                        _logger.LogWarning("No service found to handle shape type: {ShapeType}", shape.Type);
                        continue;
                    }
                    
                    // Use the starting index for each shape to ensure correct sequence
                    var shapeParameters = new WaypointParameters
                    {
                        Altitude = parameters.Altitude,
                        Speed = parameters.Speed,
                        LineSpacing = parameters.LineSpacing,
                        StartingIndex = currentIndex,
                        Action = parameters.Action,
                        PhotoInterval = parameters.PhotoInterval,
                        UseEndpointsOnly = parameters.UseEndpointsOnly,
                        IsNorthSouth = parameters.IsNorthSouth,
                        UnitType = parameters.UnitType
                    };
                    
                    var shapeWaypoints = service.GenerateWaypoints(shape, shapeParameters);
                    
                    // Update the current index for the next shape
                    if (shapeWaypoints.Count > 0)
                    {
                        currentIndex = shapeWaypoints.Max(w => w.Index) + 1;
                    }
                    
                    allWaypoints.AddRange(shapeWaypoints);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating waypoints for shape ID: {ShapeId}", shape.Id);
                }
            }
            
            _logger.LogInformation("Generated {WaypointCount} waypoints in total", allWaypoints.Count);
            
            // Use Task.FromResult since there's no actual async operation here, 
            // but keeping the interface async for future extensions
            return await Task.FromResult(allWaypoints);
        }
    }
} 