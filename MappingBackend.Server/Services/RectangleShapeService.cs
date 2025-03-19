using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Service for generating waypoints for rectangle shapes
    /// </summary>
    public class RectangleShapeService : IShapeService
    {
        private readonly IGeometryService _geometryService;
        private readonly ILogger<RectangleShapeService> _logger;

        public RectangleShapeService(IGeometryService geometryService, ILogger<RectangleShapeService> logger)
        {
            _geometryService = geometryService;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool CanHandleShapeType(string shapeType)
        {
            return shapeType?.ToLower() == ShapeTypes.Rectangle;
        }

        /// <inheritdoc />
        public List<Waypoint> GenerateWaypoints(ShapeData shape, WaypointParameters parameters)
        {
            _logger.LogInformation("Generating rectangle waypoints with parameters: Altitude={Altitude}, Speed={Speed}, UseEndpointsOnly={UseEndpointsOnly}",
                parameters.Altitude, parameters.Speed, parameters.UseEndpointsOnly);
            
            if (shape == null || shape.Coordinates == null || shape.Coordinates.Count < 2)
            {
                return new List<Waypoint>();
            }
            
            var waypoints = new List<Waypoint>();
            var coordinates = shape.Coordinates;
            
            _logger.LogInformation("Generating rectangle waypoints with {Count} coordinates", coordinates.Count);
            
            // Validate parameters and ensure we have enough coordinates for a rectangle
            if (coordinates.Count < 4)
            {
                throw new ArgumentException("Rectangle shape requires at least 4 coordinate points");
            }
            
            // Calculate camera FOV and ground coverage based on camera parameters (if provided)
            double effectiveLineSpacing = parameters.LineSpacing;
            double effectivePhotoInterval = parameters.PhotoInterval;
            
            // If we have camera parameters available, calculate optimal line spacing and photo interval
            if (parameters.SensorWidth > 0 && parameters.SensorHeight > 0 && parameters.FocalLength > 0 && parameters.Overlap > 0)
            {
                // Calculate horizontal and vertical FOV in radians
                double fovH = 2 * Math.Atan(parameters.SensorWidth / (2 * parameters.FocalLength));
                double fovV = 2 * Math.Atan(parameters.SensorHeight / (2 * parameters.FocalLength));
                
                // Calculate ground coverage dimensions
                double groundWidth = 2 * parameters.Altitude * Math.Tan(fovH / 2);
                double groundHeight = 2 * parameters.Altitude * Math.Tan(fovV / 2);
                
                // Calculate line spacing from ground width and overlap
                effectiveLineSpacing = groundWidth * (1 - parameters.Overlap / 100.0);
                
                // Calculate distance between photos
                double distanceBetweenPhotos = groundHeight * (1 - parameters.Overlap / 100.0);
                
                // If speed is not manually set, calculate it from photo interval
                if (!parameters.ManualSpeedSet && parameters.PhotoInterval > 0)
                {
                    // Speed = distance / time
                    double calculatedSpeed = distanceBetweenPhotos / parameters.PhotoInterval;
                    _logger.LogInformation("Calculated optimal speed: {Speed} m/s based on photo interval", calculatedSpeed);
                }
                else
                {
                    // If speed is manually set, adjust photo interval accordingly
                    effectivePhotoInterval = distanceBetweenPhotos / parameters.Speed;
                }
                
                _logger.LogInformation("Camera calculations: Ground Width={Width}m, Height={Height}m, Line Spacing={LineSpacing}m, Photo Distance={PhotoDistance}m",
                    groundWidth, groundHeight, effectiveLineSpacing, distanceBetweenPhotos);
            }
            
            // Determine the area boundaries (min and max lat/lng)
            double minLat = coordinates.Min(c => c.Lat);
            double maxLat = coordinates.Max(c => c.Lat);
            double minLng = coordinates.Min(c => c.Lng);
            double maxLng = coordinates.Max(c => c.Lng);
            
            int id = parameters.StartingIndex;
            
            // Generate grid pattern based on isNorthSouth and useEndpointsOnly parameters
            if (parameters.IsNorthSouth)
            {
                // North-South direction
                double lngDistanceInDegrees = effectiveLineSpacing / (111320.0 * Math.Cos(minLat * Math.PI / 180.0));  // Convert longitude to meters
                
                for (double lng = minLng; lng <= maxLng; lng += lngDistanceInDegrees)
                {
                    if (parameters.UseEndpointsOnly)
                    {
                        // Add first waypoint at the bottom of this column
                        waypoints.Add(new Waypoint(
                            id++,
                            minLat,
                            lng,
                            parameters.Altitude,
                            parameters.Speed,
                            parameters.Action
                        ) {
                            Heading = 0  // North
                        });
                        
                        // Add second waypoint at the top of this column
                        waypoints.Add(new Waypoint(
                            id++,
                            maxLat,
                            lng,
                            parameters.Altitude,
                            parameters.Speed,
                            parameters.Action
                        ) {
                            Heading = 180  // South
                        });
                        
                        // Skip the next line by adding an extra step
                        lng += lngDistanceInDegrees;
                        if (lng <= maxLng)
                        {
                            // Add waypoint at the top of the next column
                            waypoints.Add(new Waypoint(
                                id++,
                                maxLat,
                                lng,
                                parameters.Altitude,
                                parameters.Speed,
                                parameters.Action
                            ) {
                                Heading = 180  // South
                            });
                            
                            // Add waypoint at the bottom of the next column
                            waypoints.Add(new Waypoint(
                                id++,
                                minLat,
                                lng,
                                parameters.Altitude,
                                parameters.Speed,
                                parameters.Action
                            ) {
                                Heading = 0  // North
                            });
                        }
                    }
                    else
                    {
                        // Calculate the distance in meters for this column
                        double columnHeightMeters = _geometryService.CalculateDistance(minLat, lng, maxLat, lng);
                        
                        // Calculate number of waypoints based on photo interval and speed
                        double distancePerPhoto = parameters.Speed * effectivePhotoInterval;
                        int numWaypoints = Math.Max(2, (int)(columnHeightMeters / distancePerPhoto) + 1);
                        
                        _logger.LogInformation("Column height: {Height}m, Photo interval: {Interval}s, Photo distance: {Distance}m, Creating {Count} waypoints", 
                            columnHeightMeters, effectivePhotoInterval, distancePerPhoto, numWaypoints);
                        
                        // Determine if we're going north or south on this column
                        bool goingNorth = ((int)((lng - minLng) / lngDistanceInDegrees)) % 2 == 0;
                        
                        // Add waypoints along this column
                        for (int i = 0; i < numWaypoints; i++)
                        {
                            // Calculate position as a fraction from bottom to top
                            double fraction = (double)i / (numWaypoints - 1);
                            double lat;
                            
                            // If going south, reverse the order
                            if (!goingNorth)
                            {
                                fraction = 1.0 - fraction;
                            }
                            
                            // Linear interpolation between min and max latitude
                            lat = minLat + fraction * (maxLat - minLat);
                            
                            // Add waypoint
                            waypoints.Add(new Waypoint(
                                id++,
                                lat,
                                lng,
                                parameters.Altitude,
                                parameters.Speed,
                                parameters.Action
                            )
                            {
                                Heading = goingNorth ? 0 : 180  // North or South
                            });
                        }
                    }
                }
            }
            else
            {
                // East-West direction
                double latDistanceInDegrees = effectiveLineSpacing / 111320.0;  // Convert latitude to meters
                
                for (double lat = minLat; lat <= maxLat; lat += latDistanceInDegrees)
                {
                    if (parameters.UseEndpointsOnly)
                    {
                        // Add first waypoint at the left side of this row
                        waypoints.Add(new Waypoint(
                            id++,
                            lat,
                            minLng,
                            parameters.Altitude,
                            parameters.Speed,
                            parameters.Action
                        ) {
                            Heading = 90  // East
                        });
                        
                        // Add second waypoint at the right side of this row
                        waypoints.Add(new Waypoint(
                            id++,
                            lat,
                            maxLng,
                            parameters.Altitude,
                            parameters.Speed,
                            parameters.Action
                        ) {
                            Heading = 270  // West
                        });
                        
                        // Skip the next line by adding an extra step
                        lat += latDistanceInDegrees;
                        if (lat <= maxLat)
                        {
                            // Add waypoint at the right side of the next row
                            waypoints.Add(new Waypoint(
                                id++,
                                lat,
                                maxLng,
                                parameters.Altitude,
                                parameters.Speed,
                                parameters.Action
                            ) {
                                Heading = 270  // West
                            });
                            
                            // Add waypoint at the left side of the next row
                            waypoints.Add(new Waypoint(
                                id++,
                                lat,
                                minLng,
                                parameters.Altitude,
                                parameters.Speed,
                                parameters.Action
                            ) {
                                Heading = 90  // East
                            });
                        }
                    }
                    else
                    {
                        // Calculate the distance in meters for this row
                        double rowWidthMeters = _geometryService.CalculateDistance(lat, minLng, lat, maxLng);
                        
                        // Calculate number of waypoints based on photo interval
                        double distancePerPhoto = parameters.Speed * effectivePhotoInterval;
                        int numWaypoints = Math.Max(2, (int)(rowWidthMeters / distancePerPhoto) + 1);
                        
                        _logger.LogInformation("Row width: {Width}m, Photo interval: {Interval}s, Photo distance: {Distance}m, Creating {Count} waypoints", 
                            rowWidthMeters, effectivePhotoInterval, distancePerPhoto, numWaypoints);
                        
                        // Determine if we're going east or west on this row
                        bool goingEast = ((int)((lat - minLat) / latDistanceInDegrees)) % 2 == 0;
                        
                        // Add waypoints along this row
                        for (int i = 0; i < numWaypoints; i++)
                        {
                            // Calculate position as a fraction from left to right
                            double fraction = (double)i / (numWaypoints - 1);
                            double lng;
                            
                            // If going west, reverse the order
                            if (!goingEast)
                            {
                                fraction = 1.0 - fraction;
                            }
                            
                            // Linear interpolation between min and max longitude
                            lng = minLng + fraction * (maxLng - minLng);
                            
                            // Add waypoint
                            waypoints.Add(new Waypoint(
                                id++,
                                lat,
                                lng,
                                parameters.Altitude,
                                parameters.Speed,
                                parameters.Action
                            )
                            {
                                Heading = goingEast ? 90 : 270  // East or West
                            });
                        }
                    }
                }
            }
            
            _logger.LogInformation("Generated {Count} waypoints for rectangle with UseEndpointsOnly={UseEndpointsOnly}", 
                waypoints.Count, parameters.UseEndpointsOnly);
            return waypoints;
        }

        // Helper method to calculate heading from point 1 to point 2
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
    }
} 