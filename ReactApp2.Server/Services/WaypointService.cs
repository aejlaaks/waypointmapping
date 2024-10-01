using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using Waypoint = KarttaBackEnd2.Server.Models.Waypoint;

namespace KarttaBackEnd2.Server.Services
{
    public class WaypointService : IWaypointService
    {
        public async Task<List<Waypoint>> GenerateWaypointsAsync(string allPointsAction ,
         int unitType_in,
         double altitude,
         double speed,
         int angle,
         double in_distance,
         List<Coordinate> bounds,
         string boundsType,
         int in_startingIndex)
        {
            var waypoints = new List<Waypoint>();
            int id = in_startingIndex;

            // Determine the min and max latitude and longitude from the bounds
            double minLat = bounds.Min(b => b.Lat);
            double maxLat = bounds.Max(b => b.Lat);
            double minLng = bounds.Min(b => b.Lng);
            double maxLng = bounds.Max(b => b.Lng);

            // Convert vertical distance (10 meters) to degrees of latitude
            double verticalDistanceInDegrees = in_distance / 111320.0;

            // Calculate horizontal distance based on speed and time (3 seconds)
            double horizontalDistanceInMeters = speed * 15;
            double horizontalDistanceInDegrees = horizontalDistanceInMeters / 111320.0;

            bool goingEast = true;
            for (double lat = minLat; lat <= maxLat; lat += verticalDistanceInDegrees)
            {
                for (double lng = minLng; lng <= maxLng; lng += horizontalDistanceInDegrees)
                {
                    waypoints.Add(new Waypoint
                    {
                        Latitude = lat,
                        Longitude = lng,
                        Altitude = altitude,
                        Heading = goingEast ? 90 : -90, // 90 for east, -90 for west
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
                        Action = allPointsAction
                    });
                }
                // Toggle direction for the next row
                goingEast = !goingEast;
            }

            return await Task.FromResult(waypoints);
        }

        private double Distance(Coordinate point1, Coordinate point2)
        {
            const double R = 6371e3; // Earth's radius in meters
            double lat1Rad = DegreesToRadians(point1.Lat);
            double lat2Rad = DegreesToRadians(point2.Lat);
            double deltaLat = DegreesToRadians(point2.Lat - point1.Lat);
            double deltaLng = DegreesToRadians(point2.Lng - point1.Lng);

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Distance in meters
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private double Interpolate(double start, double end, int step, int totalSteps)
        {
            return start + ((end - start) * (step / (double)totalSteps));
        }
    }
}
