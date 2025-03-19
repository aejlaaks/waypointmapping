using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Interfaces
{
    /// <summary>
    /// Interface for services that generate waypoints for specific shape types
    /// </summary>
    public interface IShapeService
    {
        /// <summary>
        /// Determines whether this service can handle the specified shape type
        /// </summary>
        /// <param name="shapeType">The shape type to check</param>
        /// <returns>True if this service can handle the shape type, false otherwise</returns>
        bool CanHandleShapeType(string shapeType);
        
        /// <summary>
        /// Generates waypoints for a specific shape
        /// </summary>
        /// <param name="shape">The shape data to generate waypoints for</param>
        /// <param name="parameters">Waypoint generation parameters</param>
        /// <returns>A list of waypoints for the shape</returns>
        List<Waypoint> GenerateWaypoints(ShapeData shape, WaypointParameters parameters);
    }
} 