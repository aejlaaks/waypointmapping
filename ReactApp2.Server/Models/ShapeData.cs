using Newtonsoft.Json;

namespace KarttaBackEnd2.Server.Models
{
    /// <summary>
    /// Data model for shape information used in waypoint generation
    /// </summary>
    public class ShapeData
    {
        /// <summary>
        /// Gets or sets the unique identifier of the shape
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the type of shape (rectangle, circle, polygon, polyline)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the coordinates that define the shape
        /// For rectangles: 2 points (top-left and bottom-right corners)
        /// For circles: 1 point (center)
        /// For polygons: 3+ points (vertices)
        /// For polylines: 2+ points (line vertices)
        /// </summary>
        [JsonProperty("coordinates")]
        public List<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
        
        /// <summary>
        /// Gets or sets the radius of a circle shape (in meters)
        /// Only used for circle shapes
        /// </summary>
        [JsonProperty("radius")]
        public double Radius { get; set; }
    }
} 