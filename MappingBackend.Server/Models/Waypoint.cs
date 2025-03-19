using Newtonsoft.Json;

namespace KarttaBackEnd2.Server.Models
{
    /// <summary>
    /// Represents a waypoint for drone flight paths
    /// </summary>
    public class Waypoint
    {
        /// <summary>
        /// Gets or sets the waypoint index
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the latitude of the waypoint
        /// </summary>
        [JsonProperty("lat")]
        public double Lat { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the waypoint
        /// </summary>
        [JsonProperty("lng")]
        public double Lng { get; set; }

        /// <summary>
        /// Gets or sets the altitude of the waypoint in meters
        /// </summary>
        [JsonProperty("alt")]
        public double Alt { get; set; }

        /// <summary>
        /// Gets or sets the speed at this waypoint in m/s
        /// </summary>
        [JsonProperty("speed")]
        public double Speed { get; set; }

        /// <summary>
        /// Gets or sets the action to perform at this waypoint (e.g., take photo, start recording, etc.)
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the heading in degrees (0-360)
        /// </summary>
        [JsonProperty("heading")]
        public double Heading { get; set; }

        /// <summary>
        /// Creates a new waypoint with the specified parameters
        /// </summary>
        public Waypoint(int index, double lat, double lng, double alt, double speed, string action)
        {
            Index = index;
            Lat = lat;
            Lng = lng;
            Alt = alt;
            Speed = speed;
            Action = action;
        }

        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public Waypoint() { }
    }
}
