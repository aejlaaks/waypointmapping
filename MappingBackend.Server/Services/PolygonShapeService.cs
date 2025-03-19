using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Service for generating waypoints for polygon shapes
    /// </summary>
    public class PolygonShapeService : IShapeService
    {
        private readonly IGeometryService _geometryService;

        public PolygonShapeService(IGeometryService geometryService)
        {
            _geometryService = geometryService;
        }

        /// <inheritdoc />
        public bool CanHandleShapeType(string shapeType)
        {
            return shapeType?.ToLower() == ShapeTypes.Polygon;
        }

        /// <inheritdoc />
        public List<Waypoint> GenerateWaypoints(ShapeData shape, WaypointParameters parameters)
        {
            if (shape == null || shape.Coordinates == null || shape.Coordinates.Count < 3)
            {
                return new List<Waypoint>();
            }

            var waypoints = new List<Waypoint>();
            var polygonCoordinates = shape.Coordinates;

            // Calculate the bounding box of the polygon
            double minLat = polygonCoordinates.Min(c => c.Lat);
            double maxLat = polygonCoordinates.Max(c => c.Lat);
            double minLng = polygonCoordinates.Min(c => c.Lng);
            double maxLng = polygonCoordinates.Max(c => c.Lng);

            // Convert line spacing to degrees based on latitude
            double lineSpacingDegrees = _geometryService.MetersToDegrees(parameters.LineSpacing, minLat);

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
                        // Only add waypoints that are inside the polygon
                        if (_geometryService.IsPointInPolygon(polygonCoordinates, lat, lng))
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
                        // Only add waypoints that are inside the polygon
                        if (_geometryService.IsPointInPolygon(polygonCoordinates, lat, lng))
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