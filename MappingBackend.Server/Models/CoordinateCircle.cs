using Newtonsoft.Json;

namespace KarttaBackEnd2.Server.Models
{
    /// <summary>
    /// Represents a coordinate with a circle definition (center point and radius)
    /// </summary>
    public class CoordinateCircle
    {
        /// <summary>
        /// Gets or sets the latitude
        /// </summary>
        [JsonProperty("lat")]
        public double Lat { get; set; }

        /// <summary>
        /// Gets or sets the longitude
        /// </summary>
        [JsonProperty("lng")]
        public double Lng { get; set; }

        /// <summary>
        /// Gets or sets the radius of the circle in meters
        /// </summary>
        [JsonProperty("radius")]
        public double Radius { get; set; }

        /// <summary>
        /// Gets or sets the type of shape (typically "circle")
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
