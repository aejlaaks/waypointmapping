using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Interfaces
{
    /// <summary>
    /// Service for geometric calculations and operations
    /// </summary>
    public interface IGeometryService
    {
        /// <summary>
        /// Checks if a point is inside a polygon
        /// </summary>
        /// <param name="polygon">List of coordinates forming a polygon</param>
        /// <param name="lat">Latitude of the point</param>
        /// <param name="lng">Longitude of the point</param>
        /// <returns>True if the point is inside the polygon</returns>
        bool IsPointInPolygon(List<Coordinate> polygon, double lat, double lng);
        
        /// <summary>
        /// Checks if point q lies on segment pr
        /// </summary>
        bool OnSegment(Coordinate p, Coordinate q, Coordinate r);
        
        /// <summary>
        /// Calculates the orientation of 3 points
        /// </summary>
        /// <returns>0: collinear, 1: clockwise, 2: counterclockwise</returns>
        int Orientation(Coordinate p, Coordinate q, Coordinate r);
        
        /// <summary>
        /// Calculates the distance between two coordinates in meters
        /// </summary>
        double CalculateDistance(double lat1, double lng1, double lat2, double lng2);
        
        /// <summary>
        /// Converts a distance in degrees to meters at the given latitude
        /// </summary>
        double DegreesToMeters(double degrees, double latitude);
        
        /// <summary>
        /// Converts a distance in meters to degrees at the given latitude
        /// </summary>
        double MetersToDegrees(double meters, double latitude);
    }
} 