using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Service for generating waypoints for circle shapes
    /// </summary>
    public class CircleShapeService : IShapeService
    {
        private readonly IGeometryService _geometryService;
        private readonly ILogger<CircleShapeService> _logger;

        public CircleShapeService(IGeometryService geometryService, ILogger<CircleShapeService> logger)
        {
            _geometryService = geometryService;
            _logger = logger;
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
            
            _logger.LogInformation("Generating circle waypoints. Center: ({Lat}, {Lng}), Radius: {Radius}m",
                center.Lat, center.Lng, radiusMeters);

            // Log detailed parameters for debugging
            _logger.LogInformation("Circle parameters: PhotoInterval={PhotoInterval}, Speed={Speed}, LineSpacing={LineSpacing}",
                parameters.PhotoInterval, parameters.Speed, parameters.LineSpacing);
            
            // Log the center coordinates explicitly - these are the actual map coordinates
            _logger.LogInformation("Center coordinates: Lat={CenterLat}, Lng={CenterLng}", center.Lat, center.Lng);
            
            // Explicitly verify the radius
            _logger.LogInformation("Circle radius: {Radius} meters", radiusMeters);
            
            // Handle the center coordinates validation with more info
            if (center.Lat == 0 && center.Lng == 0)
            {
                _logger.LogWarning("Center at (0,0) - CRITICAL ERROR: Circle center is at (0,0) which is likely incorrect. Actual center values: Lat={Lat}, Lng={Lng}, Radius={Radius}", 
                    center.Lat, center.Lng, radiusMeters);
            }

            // Always log the center coordinates 
            _logger.LogInformation("Circle center raw coordinate values: Center.Lat={CenterLat}, Center.Lng={CenterLng}, Center.Radius={Radius}",
                center.Lat, center.Lng, radiusMeters);

            // If center coordinates are suspiciously small (near 0,0), log a warning
            if (Math.Abs(center.Lat) < 1 && Math.Abs(center.Lng) < 1)
            {
                _logger.LogWarning("CAUTION: Center coordinates are suspiciously small. They might be relative offsets instead of absolute coordinates.");
                
                // Attempt to use a default center if the coordinates are likely invalid
                // This is a fallback for testing only - Helsinki coordinates
                if (Math.Abs(center.Lat) < 0.001 && Math.Abs(center.Lng) < 0.001)
                {
                    double defaultLat = 60.1699;
                    double defaultLng = 24.9384;
                    
                    _logger.LogWarning("Coordinates near (0,0) detected! Using default center at Helsinki ({DefaultLat}, {DefaultLng})",
                        defaultLat, defaultLng);
                        
                    // Use the default coordinates for testing
                    // IMPORTANT: Comment this out in production - this is just to test rendering
                    center.Lat = defaultLat;
                    center.Lng = defaultLng;
                }
            }

            // Use the circle algorithm with the provided center coordinates
            double centerLat = center.Lat;
            double centerLng = center.Lng;
            
            // Calculate the number of waypoints based on circumference and speed
            double circumference = 2 * Math.PI * radiusMeters;
            double distancePerWaypoint = parameters.Speed * parameters.PhotoInterval;
            int numberOfWaypoints = Math.Max(24, (int)(circumference / distancePerWaypoint));
            
            _logger.LogInformation("Creating {Count} waypoints for circle", numberOfWaypoints);
            
            // Generate waypoints using an approach matching Google Maps' circle display
            // For perfect circles, we need at least 24 points for smooth appearance
            double angleStep = 360.0 / numberOfWaypoints;
            int id = parameters.StartingIndex;

            for (int i = 0; i < numberOfWaypoints; i++)
            {
                // Calculate angle for this waypoint in degrees
                double angle = i * angleStep;
                double angleRad = angle * (Math.PI / 180.0);
                
                // Calculate heading from center to this point on circle (90 degrees offset from angle)
                double headingFromCenter = (angle + 90) % 360;
                double headingRadians = headingFromCenter * (Math.PI / 180.0);
                
                // Use the same formula Google Maps uses for geodesic circles
                // This ensures waypoints exactly match the displayed circle
                double angularDistance = radiusMeters / 6378137.0; // Earth radius in meters
                
                double startLatRad = centerLat * (Math.PI / 180.0);
                double startLonRad = centerLng * (Math.PI / 180.0);
                
                // Calculate endpoint using spherical law of cosines
                double endLatRad = Math.Asin(
                    Math.Sin(startLatRad) * Math.Cos(angularDistance) +
                    Math.Cos(startLatRad) * Math.Sin(angularDistance) * Math.Cos(headingRadians)
                );
                
                double endLonRad = startLonRad + Math.Atan2(
                    Math.Sin(headingRadians) * Math.Sin(angularDistance) * Math.Cos(startLatRad),
                    Math.Cos(angularDistance) - Math.Sin(startLatRad) * Math.Sin(endLatRad)
                );
                
                // Convert back to degrees
                double waypointLat = endLatRad * (180.0 / Math.PI);
                double waypointLng = endLonRad * (180.0 / Math.PI);
                
                // Calculate the heading toward the center (reverse of heading from center)
                double headingToCenter = (headingFromCenter + 180.0) % 360.0;
                
                var waypoint = new Waypoint(
                    id++,
                    waypointLat,
                    waypointLng,
                    parameters.Altitude,
                    parameters.Speed,
                    parameters.Action
                );
                
                waypoint.Heading = headingToCenter;
                waypoints.Add(waypoint);
            }
            
            // Log the first waypoint for debugging
            if (waypoints.Count > 0)
            {
                var firstWp = waypoints[0];
                _logger.LogInformation("First waypoint: Lat={Lat}, Lng={Lng}, Heading={Heading}",
                    firstWp.Lat, firstWp.Lng, firstWp.Heading);
                    
                // Calculate and log the distance from center to first waypoint to verify radius
                double distance = _geometryService.CalculateDistance(
                    center.Lat, center.Lng, firstWp.Lat, firstWp.Lng);
                _logger.LogInformation("Distance from center to first waypoint: {Distance}m (should be close to radius: {Radius}m)",
                    distance, radiusMeters);
            }

            _logger.LogInformation("Generated {Count} waypoints for circle", waypoints.Count);
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
            
            // Create waypoint with default heading (will be set in the calling method)
            var waypoint = new Waypoint(
                index,
                lat,
                lng,
                parameters.Altitude,
                parameters.Speed,
                action
            );
            
            return waypoint;
        }
    }
} 