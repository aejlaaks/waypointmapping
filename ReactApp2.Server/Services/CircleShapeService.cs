using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Service for generating waypoints for circle shapes
    /// </summary>
    public class CircleShapeService : IShapeService
    {
        private readonly IGeometryService _geometryService;

        public CircleShapeService(IGeometryService geometryService)
        {
            _geometryService = geometryService;
        }

        /// <inheritdoc />
        public bool CanHandleShapeType(string shapeType)
        {
            return shapeType?.ToLower() == ShapeTypes.Circle;
        }

        /// <inheritdoc />
        public List<Waypoint> GenerateWaypoints(ShapeData shape, WaypointParameters parameters)
        {
            if (shape == null || shape.Coordinates == null || shape.Coordinates.Count < 1)
            {
                return new List<Waypoint>();
            }

            var waypoints = new List<Waypoint>();
            var center = shape.Coordinates[0];
            double radiusMeters = shape.Radius;
            
            // Convert radius and line spacing to degrees based on latitude
            double radiusDegrees = _geometryService.MetersToDegrees(radiusMeters, center.Lat);
            double lineSpacingDegrees = _geometryService.MetersToDegrees(parameters.LineSpacing, center.Lat);

            // Calculate the bounding box of the circle
            double minLat = center.Lat - radiusDegrees;
            double maxLat = center.Lat + radiusDegrees;
            double minLng = center.Lng - radiusDegrees;
            double maxLng = center.Lng + radiusDegrees;

            // Determine the direction of the flight lines
            if (parameters.IsNorthSouth)
            {
                // North-South lines (moving east after each column)
                int waypointIndex = parameters.StartingIndex;
                int lineCount = 0;
                
                for (double lng = minLng; lng <= maxLng; lng += lineSpacingDegrees)
                {
                    var lineWaypoints = new List<Waypoint>();
                    bool isEvenLine = lineCount % 2 == 0;
                    double startLat = isEvenLine ? minLat : maxLat;
                    double endLat = isEvenLine ? maxLat : minLat;
                    double step = isEvenLine ? lineSpacingDegrees / 10 : -lineSpacingDegrees / 10;
                    
                    for (double lat = startLat; isEvenLine ? lat <= endLat : lat >= endLat; lat += step)
                    {
                        // Calculate distance from center
                        double distance = _geometryService.CalculateDistance(
                            center.Lat, center.Lng, lat, lng);
                        
                        // Only add waypoints that are inside the circle
                        if (distance <= radiusMeters)
                        {
                            lineWaypoints.Add(CreateWaypoint(waypointIndex++, lat, lng, parameters));
                        }
                    }
                    
                    // Only add if we have waypoints in this line
                    if (lineWaypoints.Count > 0)
                    {
                        waypoints.AddRange(lineWaypoints);
                        lineCount++;
                    }
                }
            }
            else
            {
                // East-West lines (moving north after each row)
                int waypointIndex = parameters.StartingIndex;
                int lineCount = 0;
                
                for (double lat = minLat; lat <= maxLat; lat += lineSpacingDegrees)
                {
                    var lineWaypoints = new List<Waypoint>();
                    bool isEvenLine = lineCount % 2 == 0;
                    double startLng = isEvenLine ? minLng : maxLng;
                    double endLng = isEvenLine ? maxLng : minLng;
                    double step = isEvenLine ? lineSpacingDegrees / 10 : -lineSpacingDegrees / 10;
                    
                    for (double lng = startLng; isEvenLine ? lng <= endLng : lng >= endLng; lng += step)
                    {
                        // Calculate distance from center
                        double distance = _geometryService.CalculateDistance(
                            center.Lat, center.Lng, lat, lng);
                        
                        // Only add waypoints that are inside the circle
                        if (distance <= radiusMeters)
                        {
                            lineWaypoints.Add(CreateWaypoint(waypointIndex++, lat, lng, parameters));
                        }
                    }
                    
                    // Only add if we have waypoints in this line
                    if (lineWaypoints.Count > 0)
                    {
                        waypoints.AddRange(lineWaypoints);
                        lineCount++;
                    }
                }
            }

            return waypoints;
        }

        private Waypoint CreateWaypoint(int index, double lat, double lng, WaypointParameters parameters)
        {
            string action = parameters.Action;
            
            // Handle photo interval if specified
            if (parameters.PhotoInterval > 0 && index % parameters.PhotoInterval == 0)
            {
                action = WaypointActions.TakePhoto;
            }
            
            return new Waypoint(
                index,
                lat,
                lng,
                parameters.Altitude,
                parameters.Speed,
                action
            );
        }
    }
} 