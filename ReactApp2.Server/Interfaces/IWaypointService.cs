using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Interfaces

{
    public interface IWaypointService
    {
        Task<List<Waypoint>> GenerateWaypointsAsync(string action, int unitType_in, double
            altitude, double speed, int angle, double in_distance, List<Coordinate> bounds, List<CoordinateCircle> shapes, string boundsType,
            int in_startingIndex, 
    double photoInterval);
    }
}