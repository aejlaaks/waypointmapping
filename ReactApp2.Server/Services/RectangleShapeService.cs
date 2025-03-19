using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Service for generating waypoints for rectangle shapes
    /// </summary>
    public class RectangleShapeService : IShapeService
    {
        private readonly IGeometryService _geometryService;

        public RectangleShapeService(IGeometryService geometryService)
        {
            _geometryService = geometryService;
        }

        /// <inheritdoc />
        public bool CanHandleShapeType(string shapeType)
        {
            return shapeType?.ToLower() == ShapeTypes.Rectangle;
        }

        /// <inheritdoc />
        public List<Waypoint> GenerateWaypoints(ShapeData shape, WaypointParameters parameters)
        {
            if (shape == null || shape.Coordinates == null || shape.Coordinates.Count < 2)
            {
                return new List<Waypoint>();
            }

            var waypoints = new List<Waypoint>();
            var boundsCoordinates = shape.Coordinates;

            if (boundsCoordinates.Count < 2)
            {
                return waypoints;
            }

            // For a rectangle, we need the southwest and northeast corners
            var sw = boundsCoordinates[0];
            var ne = boundsCoordinates[1];

            double minLat = sw.Lat;
            double maxLat = ne.Lat;
            double minLng = sw.Lng;
            double maxLng = ne.Lng;

            // Convert line spacing to degrees based on latitude
            double lineSpacingDegrees = _geometryService.MetersToDegrees(parameters.LineSpacing, minLat);
            
            // Determine the direction of the flight lines
            if (parameters.IsNorthSouth)
            {
                // North-South lines (moving east after each column)
                int waypointIndex = parameters.StartingIndex;
                
                for (double lng = minLng; lng <= maxLng; lng += lineSpacingDegrees)
                {
                    // Alternate up and down to create an efficient path
                    if ((int)((lng - minLng) / lineSpacingDegrees) % 2 == 0)
                    {
                        // Going North
                        for (double lat = minLat; lat <= maxLat; lat += lineSpacingDegrees / 10)
                        {
                            waypoints.Add(CreateWaypoint(waypointIndex++, lat, lng, parameters));
                        }
                    }
                    else
                    {
                        // Going South
                        for (double lat = maxLat; lat >= minLat; lat -= lineSpacingDegrees / 10)
                        {
                            waypoints.Add(CreateWaypoint(waypointIndex++, lat, lng, parameters));
                        }
                    }
                }
            }
            else
            {
                // East-West lines (moving north after each row)
                int waypointIndex = parameters.StartingIndex;
                
                for (double lat = minLat; lat <= maxLat; lat += lineSpacingDegrees)
                {
                    // Alternate left and right to create an efficient path
                    if ((int)((lat - minLat) / lineSpacingDegrees) % 2 == 0)
                    {
                        // Going East
                        for (double lng = minLng; lng <= maxLng; lng += lineSpacingDegrees / 10)
                        {
                            waypoints.Add(CreateWaypoint(waypointIndex++, lat, lng, parameters));
                        }
                    }
                    else
                    {
                        // Going West
                        for (double lng = maxLng; lng >= minLng; lng -= lineSpacingDegrees / 10)
                        {
                            waypoints.Add(CreateWaypoint(waypointIndex++, lat, lng, parameters));
                        }
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