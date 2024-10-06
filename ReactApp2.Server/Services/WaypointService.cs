using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using SharpKml.Dom;
using Waypoint = KarttaBackEnd2.Server.Models.Waypoint;

namespace KarttaBackEnd2.Server.Services
{
    public class WaypointService : IWaypointService
    {
        // Method to generate waypoints
        public async Task<List<Waypoint>> GenerateWaypointsAsync(string allPointsAction,
            int unitType_in,
            double altitude,
            double speed,  // Speed in meters/second
            int angle,
            double in_distance,
            List<Coordinate> bounds,  // Rectangle coordinates come from 'bounds'
            List<CoordinateCircle> shapes,  // Circle coordinates come from 'shapes'
            string boundsType,
            int in_startingIndex,
            double photoInterval = 0)  // Photo interval in seconds
        {
            var waypoints = new List<Waypoint>();
            int id = in_startingIndex;

            if (boundsType == "rectangle")
            {
                // Existing logic for rectangular bounds using 'bounds' variable
                double minLat = bounds.Min(b => b.Lat);
                double maxLat = bounds.Max(b => b.Lat);
                double minLng = bounds.Min(b => b.Lng);
                double maxLng = bounds.Max(b => b.Lng);

                double verticalDistanceInDegrees = in_distance / 111320.0;
                double horizontalDistanceInDegrees = (maxLng - minLng) / (bounds.Count - 1);

                bool goingEast = true;
                for (double lat = minLat; lat <= maxLat; lat += verticalDistanceInDegrees)
                {
                    double currentLng = goingEast ? minLng : maxLng;
                    double endLng = goingEast ? maxLng : minLng;
                    double step = goingEast ? horizontalDistanceInDegrees : -horizontalDistanceInDegrees;

                    while ((goingEast && currentLng <= endLng) || (!goingEast && currentLng >= endLng))
                    {
                        waypoints.Add(new Waypoint
                        {
                            Latitude = lat,
                            Longitude = currentLng,
                            Altitude = altitude,
                            Heading = goingEast ? 90 : -90,  // 90 for east, -90 for west
                            GimbalAngle = angle,
                            Speed = speed,
                            Id = id++,  // Increment Id for each waypoint
                            Action = allPointsAction
                        });

                        currentLng += step;
                    }

                    goingEast = !goingEast; // Alternate the direction
                }
            }
            else if (boundsType == "circle")
            {
                // New logic for circle bounds using 'shapes' variable
                foreach (var shape in shapes)
                {
                    // Assuming the shape contains center coordinates and radius for the circle
                    double centerLat = shape.Lat;
                    double centerLon = shape.Lng;
                    double radius = shape.Radius;  // Radius in meters

                    // Calculate the number of waypoints based on the circle's circumference and the distance per photo interval
                    double circumference = 2 * Math.PI * radius;  // Circumference of the circle
                    double distancePerPhoto = speed * photoInterval;  // Distance traveled per photo interval
                    int numberOfWaypoints = (int)(circumference / distancePerPhoto);  // Calculate number of waypoints

                    // Generate waypoints for the circle
                    var circleWaypoints = await GenerateWaypointsForCircleAsync(centerLat, centerLon, radius, altitude, speed, allPointsAction, id, photoInterval);
                    waypoints.AddRange(circleWaypoints);
                }
            }

            return await Task.FromResult(waypoints);  // Return wrapped in Task
        }

        // Method to generate waypoints for a circle
        private async Task<List<Waypoint>> GenerateWaypointsForCircleAsync(double centerLat, double centerLon, double radius, double altitude, double speed, string allPointsAction, int startingId, double photoInterval)
        {
            var waypoints = new List<Waypoint>();
            double circumference = 2 * Math.PI * radius;  // Circumference of the circle
            double distancePerPhoto = speed * photoInterval;  // Distance traveled per photo interval
            int numberOfWaypoints = (int)(circumference / distancePerPhoto);  // Calculate number of waypoints

            double angleStep = 360.0 / numberOfWaypoints;  // Divide the circle into equal angles
            double distanceCovered = 0;  // Track distance covered between waypoints
            int id = startingId;  // Initialize waypoint Id


            for (int i = 0; i < numberOfWaypoints; i++)
            {
                double angle = i * angleStep;  // Calculate the angle for the current waypoint

                // Convert angle to radians
                double angleRad = angle * (Math.PI / 180.0);

                // Calculate waypoint's latitude and longitude using the Haversine formula
                double waypointLat = centerLat + (radius / 111000.0) * Math.Cos(angleRad);  // Latitude displacement
                double waypointLon = centerLon + (radius / (111000.0 * Math.Cos(centerLat * Math.PI / 180.0))) * Math.Sin(angleRad);  // Longitude displacement

                if (distanceCovered >= distancePerPhoto)
                {
                    // Calculate heading towards the center of the circle
                    double deltaLat = centerLat - waypointLat;
                    double deltaLon = centerLon - waypointLon;
                    double heading = Math.Atan2(deltaLon, deltaLat) * (180.0 / Math.PI);  // Convert from radians to degrees

                    // Create and add the waypoint
                    waypoints.Add(new Waypoint
                    {
                        Latitude = waypointLat,
                        Longitude = waypointLon,
                        Altitude = altitude,
                        Heading = heading,  // Heading is towards the center
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,  // Increment Id for each waypoint
                        Action = allPointsAction
                    });

                    distanceCovered = 0;  // Reset distance covered after generating a waypoint
                }

                distanceCovered += (2 * Math.PI * radius) / numberOfWaypoints;  // Approximate the distance covered along the circle
            }

            return await Task.FromResult(waypoints);  // Return wrapped in Task
        }
    }

}
