using KarttaBackEnd2.Server.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KarttaBackEnd2.Server.DTOs
{
    /// <summary>
    /// Data Transfer Object for waypoint generation requests
    /// </summary>
    public class GeneratePointsRequestDTO
    {
        /// <summary>
        /// Gets or sets the unit type for measurements (0 = Metric, 1 = Imperial)
        /// </summary>
        public int UnitType { get; set; }

        /// <summary>
        /// Gets or sets the action to perform at waypoints
        /// Legacy property, prefer using AllPointsAction
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the altitude in meters
        /// </summary>
        public double Altitude { get; set; }

        /// <summary>
        /// Gets or sets the speed in meters per second
        /// </summary>
        public double Speed { get; set; }

        /// <summary>
        /// Gets or sets the distance between lines in meters
        /// Legacy property, prefer using LineSpacing
        /// </summary>
        [JsonProperty("distance")]
        [Obsolete("Use LineSpacing instead")]
        public double Distance { get; set; }

        /// <summary>
        /// Gets or sets the distance between lines in meters
        /// </summary>
        [JsonProperty("lineSpacing")]
        public double LineSpacing { get; set; }

        /// <summary>
        /// Gets or sets the interval for photo capture
        /// Legacy property, prefer using PhotoInterval
        /// </summary>
        [JsonProperty("interval")]
        [Obsolete("Use PhotoInterval instead")]
        public double Interval { get; set; }

        /// <summary>
        /// Gets or sets the bounds coordinates
        /// </summary>
        public List<Coordinate> Bounds { get; set; }

        /// <summary>
        /// Gets or sets the type of bounds (rectangle, circle, polygon, polyline)
        /// </summary>
        public string BoundsType { get; set; }

        /// <summary>
        /// Gets or sets the starting index for waypoint numbering
        /// </summary>
        public int StartingIndex { get; set; }

        /// <summary>
        /// Gets or sets the action to apply to all points
        /// </summary>
        [JsonProperty("allPointsAction")]
        public string AllPointsAction { get; set; }

        /// <summary>
        /// Gets or sets whether to use only endpoints when generating waypoints
        /// </summary>
        [JsonProperty("useEndpointsOnly")]
        public bool UseEndpointsOnly { get; set; }

        /// <summary>
        /// Gets or sets whether to use north-south pattern
        /// Legacy property with lowercase first letter, prefer using IsNorthSouth
        /// </summary>
        [JsonProperty("isNorthSouth")]
        [Obsolete("Use IsNorthSouth instead")]
        public bool isNorthSouth { get; set; }

        /// <summary>
        /// Gets or sets whether to use north-south pattern (true) or east-west pattern (false)
        /// </summary>
        [JsonProperty("isNorthSouth")]
        public bool IsNorthSouth { get; set; }

        /// <summary>
        /// Gets or sets the photo interval (how many waypoints between photos)
        /// </summary>
        [JsonProperty("photoInterval")]
        public int PhotoInterval { get; set; }
    }
}
