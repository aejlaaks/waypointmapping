using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Interfaces
{
    /// <summary>
    /// Interface for waypoint generation services
    /// </summary>
    public interface IWaypointService
    {
        /// <summary>
        /// Legacy method for generating waypoints - will be deprecated in future versions
        /// </summary>
        /// <returns>List of generated waypoints</returns>
        Task<List<Waypoint>> GenerateWaypointsAsync(
            string action,
            int unitType_in,
            double altitude,
            double speed,
            int angle,
            double in_distance,
            List<Coordinate> bounds,
            string boundsType,
            int in_startingIndex,
            double photoInterval = 3,
            bool useEndpointsOnly = false,
            bool isNorthSouth = false);

        /// <summary>
        /// Generates waypoints based on the provided shapes and parameters
        /// </summary>
        /// <param name="shapes">List of shapes to process</param>
        /// <param name="parameters">Parameters for waypoint generation</param>
        /// <returns>List of generated waypoints</returns>
        Task<List<Waypoint>> GenerateWaypointsAsync(List<ShapeData> shapes, WaypointParameters parameters);
    }
}