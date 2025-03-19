using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Services
{
    public class GeometryService : IGeometryService
    {
        private const double EarthRadius = 6378137.0; // Earth radius in meters (WGS-84)

        /// <inheritdoc />
        public bool IsPointInPolygon(List<Coordinate> polygon, double lat, double lng)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            int n = polygon.Count;
            bool isInside = false;
            
            // Use ray casting algorithm to determine if point is inside polygon
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((polygon[i].Lat > lat) != (polygon[j].Lat > lat) &&
                    (lng < (polygon[j].Lng - polygon[i].Lng) * (lat - polygon[i].Lat) / 
                    (polygon[j].Lat - polygon[i].Lat) + polygon[i].Lng))
                {
                    isInside = !isInside;
                }
            }
            
            return isInside;
        }

        /// <inheritdoc />
        public bool OnSegment(Coordinate p, Coordinate q, Coordinate r)
        {
            return q.Lng <= Math.Max(p.Lng, r.Lng) && 
                   q.Lng >= Math.Min(p.Lng, r.Lng) &&
                   q.Lat <= Math.Max(p.Lat, r.Lat) && 
                   q.Lat >= Math.Min(p.Lat, r.Lat);
        }

        /// <inheritdoc />
        public int Orientation(Coordinate p, Coordinate q, Coordinate r)
        {
            double val = (q.Lat - p.Lat) * (r.Lng - q.Lng) - (q.Lng - p.Lng) * (r.Lat - q.Lat);
            
            if (Math.Abs(val) < 1e-10)
                return 0; // Collinear
            
            return (val > 0) ? 1 : 2; // Clockwise or Counterclockwise
        }

        /// <inheritdoc />
        public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            // Use Haversine formula for great-circle distance
            double dLat = ToRadians(lat2 - lat1);
            double dLng = ToRadians(lng2 - lng1);
            
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadius * c; // Distance in meters
        }

        /// <inheritdoc />
        public double DegreesToMeters(double degrees, double latitude)
        {
            // At equator, 1 degree = 111.32 km
            // This formula adjusts for latitude
            double latRadians = ToRadians(latitude);
            double metersPerDegree = EarthRadius * Math.Cos(latRadians) * Math.PI / 180.0;
            return degrees * metersPerDegree;
        }

        /// <inheritdoc />
        public double MetersToDegrees(double meters, double latitude)
        {
            double latRadians = ToRadians(latitude);
            double metersPerDegree = EarthRadius * Math.Cos(latRadians) * Math.PI / 180.0;
            
            if (metersPerDegree < 1e-10) // Near poles, avoid division by zero
                return 0;
                
            return meters / metersPerDegree;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
} 