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
        [JsonProperty("UnitType")]
        public int UnitType { get; set; }

        /// <summary>
        /// Gets or sets the action to perform at waypoints
        /// Legacy property, prefer using AllPointsAction
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the altitude in meters
        /// </summary>
        [JsonProperty("Altitude")]
        public double Altitude { get; set; }

        /// <summary>
        /// Gets or sets the speed in meters per second
        /// </summary>
        [JsonProperty("Speed")]
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
        [JsonProperty("LineSpacing")]
        public double LineSpacing { get; set; }

        /// <summary>
        /// Gets or sets the interval for photo capture
        /// Legacy property, prefer using PhotoInterval
        /// </summary>
        [JsonProperty("interval")]
        [Obsolete("Use PhotoInterval instead")]
        public double Interval { get; set; }

        /// <summary>
        /// Gets or sets the list of coordinate points that define the bounds of the shape.
        /// </summary>
        [JsonProperty("Bounds")]
        public List<Coordinate> Bounds { get; set; }

        /// <summary>
        /// Gets or sets the type of bounds (e.g., "rectangle", "polygon").
        /// </summary>
        [JsonProperty("BoundsType")]
        public string BoundsType { get; set; }

        /// <summary>
        /// Gets or sets the starting index for waypoint IDs.
        /// </summary>
        [JsonProperty("StartingIndex")]
        public int StartingIndex { get; set; }

        /// <summary>
        /// Gets or sets the action for all points.
        /// </summary>
        [JsonProperty("AllPointsAction")]
        public string AllPointsAction { get; set; }

        /// <summary>
        /// Gets or sets whether to use only endpoints when generating waypoints.
        /// </summary>
        [JsonProperty("UseEndpointsOnly")]
        public bool UseEndpointsOnly { get; set; }

        /// <summary>
        /// Gets or sets whether to use north-south direction.
        /// </summary>
        [JsonProperty("IsNorthSouth")]
        public bool IsNorthSouth { get; set; }

        /// <summary>
        /// Gets or sets the photo interval.
        /// </summary>
        [JsonProperty("PhotoInterval")]
        public double PhotoInterval { get; set; }

        /// <summary>
        /// Gets or sets the overlap percentage.
        /// </summary>
        [JsonProperty("Overlap")]
        public double Overlap { get; set; }

        /// <summary>
        /// Gets or sets the final action.
        /// </summary>
        [JsonProperty("FinalAction")]
        public string FinalAction { get; set; }

        /// <summary>
        /// Gets or sets the angle for the waypoints.
        /// </summary>
        [JsonProperty("Angle")]
        public double Angle { get; set; }

        /// <summary>
        /// Gets or sets whether to flip the path.
        /// </summary>
        [JsonProperty("FlipPath")]
        public bool FlipPath { get; set; }

        /// <summary>
        /// Gets or sets the camera focal length in millimeters.
        /// </summary>
        [JsonProperty("FocalLength")]
        public double FocalLength { get; set; }

        /// <summary>
        /// Gets or sets the camera sensor width in millimeters.
        /// </summary>
        [JsonProperty("SensorWidth")]
        public double SensorWidth { get; set; }

        /// <summary>
        /// Gets or sets the camera sensor height in millimeters.
        /// </summary>
        [JsonProperty("SensorHeight")]
        public double SensorHeight { get; set; }

        /// <summary>
        /// Gets or sets whether the speed was manually set.
        /// </summary>
        [JsonProperty("ManualSpeedSet")]
        public bool ManualSpeedSet { get; set; }
    }
}
