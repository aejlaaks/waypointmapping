using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using Microsoft.Extensions.Logging;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Service that handles waypoint generation for polyline shapes
    /// </summary>
    public class PolylineShapeService : IShapeService
    {
        private readonly ILogger<PolylineShapeService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineShapeService"/> class.
        /// </summary>
        /// <param name="logger">Logger for the polyline shape service</param>
        public PolylineShapeService(ILogger<PolylineShapeService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public bool CanHandleShapeType(string shapeType)
        {
            return shapeType == ShapeTypes.Polyline;
        }

        /// <inheritdoc />
        public List<Waypoint> GenerateWaypoints(ShapeData shape, WaypointParameters parameters)
        {
            _logger.LogInformation("Generating waypoints for polyline with {CoordinateCount} coordinates", 
                shape.Coordinates?.Count ?? 0);
            
            var waypoints = new List<Waypoint>();
            int id = parameters.StartingIndex;

            if (shape.Coordinates == null || shape.Coordinates.Count < 1)
            {
                return waypoints;
            }

            // If only one coordinate, just add a waypoint at that location
            if (shape.Coordinates.Count == 1)
            {
                waypoints.Add(CreateWaypoint(
                    shape.Coordinates[0].Lat,
                    shape.Coordinates[0].Lng,
                    parameters.Altitude,
                    0, // Heading doesn't matter for a single point
                    parameters.Speed,
                    ref id,
                    parameters.Action
                ));
                return waypoints;
            }

            // Simple case: just add waypoints at each coordinate if useEndpointsOnly is true
            if (parameters.UseEndpointsOnly)
            {
                foreach (var coordinate in shape.Coordinates)
                {
                    waypoints.Add(CreateWaypoint(
                        coordinate.Lat,
                        coordinate.Lng,
                        parameters.Altitude,
                        0, // We'll calculate heading later if needed
                        parameters.Speed,
                        ref id,
                        parameters.Action
                    ));
                }

                // Update headings for all waypoints except the last one
                for (int i = 0; i < waypoints.Count - 1; i++)
                {
                    double heading = CalculateHeading(
                        waypoints[i].Lat, waypoints[i].Lng,
                        waypoints[i + 1].Lat, waypoints[i + 1].Lng
                    );
                    waypoints[i].Heading = heading;
                }

                // Last waypoint heading is the same as the second-to-last
                if (waypoints.Count > 1)
                {
                    waypoints[waypoints.Count - 1].Heading = waypoints[waypoints.Count - 2].Heading;
                }

                return waypoints;
            }

            // Generate waypoints along each segment based on distance and photo interval
            for (int i = 0; i < shape.Coordinates.Count - 1; i++)
            {
                var start = shape.Coordinates[i];
                var end = shape.Coordinates[i + 1];
                
                // Calculate heading
                double heading = CalculateHeading(start.Lat, start.Lng, end.Lat, end.Lng);
                
                // Calculate segment distance in meters
                double segmentDistance = CalculateDistance(start.Lat, start.Lng, end.Lat, end.Lng);
                
                // Calculate number of points to add based on segment distance and desired interval
                // photoInterval is in seconds, speed is in m/s, so photoInterval * speed = distance between photos
                double photoSpacing = parameters.Speed * parameters.PhotoInterval;
                
                // Use at least 2 points (start and end) for each segment
                int numPoints = Math.Max(2, (int)(segmentDistance / photoSpacing) + 1);
                
                _logger.LogDebug("Segment {SegmentIndex} distance: {Distance}m, adding {NumPoints} waypoints", 
                    i, segmentDistance, numPoints);
                
                // Add waypoints along the segment
                for (int j = 0; j < numPoints; j++)
                {
                    // Skip adding the endpoint of this segment if it's not the last segment
                    // (to avoid duplicate waypoints where segments connect)
                    if (j == numPoints - 1 && i < shape.Coordinates.Count - 2)
                    {
                        continue;
                    }
                    
                    double t = j / (double)(numPoints - 1);
                    double lat = start.Lat + t * (end.Lat - start.Lat);
                    double lng = start.Lng + t * (end.Lng - start.Lng);
                    
                    waypoints.Add(CreateWaypoint(lat, lng, parameters.Altitude, heading, parameters.Speed, ref id, parameters.Action));
                }
            }

            return waypoints;
        }

        /// <summary>
        /// Creates a waypoint with the given parameters
        /// </summary>
        private Waypoint CreateWaypoint(double lat, double lng, double altitude, double heading, double speed, ref int id, string action)
        {
            return new Waypoint
            {
                Lat = lat,
                Lng = lng,
                Alt = altitude,
                Heading = heading,
                Speed = speed,
                Index = id++,
                Action = action
            };
        }

        /// <summary>
        /// Calculates the heading in degrees from point 1 to point 2
        /// </summary>
        private double CalculateHeading(double lat1, double lng1, double lat2, double lng2)
        {
            // Convert to radians
            double lat1Rad = lat1 * Math.PI / 180.0;
            double lng1Rad = lng1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            double lng2Rad = lng2 * Math.PI / 180.0;
            
            // Calculate heading
            double y = Math.Sin(lng2Rad - lng1Rad) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                      Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(lng2Rad - lng1Rad);
            double heading = Math.Atan2(y, x) * 180.0 / Math.PI;
            
            // Normalize to 0-360
            return (heading + 360) % 360;
        }

        /// <summary>
        /// Calculates the distance between two coordinates in meters using the Haversine formula
        /// </summary>
        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadius = 6371000; // meters
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLng = (lng2 - lng1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }
    }
} 