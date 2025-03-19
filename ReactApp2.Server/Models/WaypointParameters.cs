namespace KarttaBackEnd2.Server.Models
{
    /// <summary>
    /// Parameter object for waypoint generation to replace long parameter lists
    /// </summary>
    public class WaypointParameters
    {
        /// <summary>
        /// The altitude of the waypoints in meters
        /// </summary>
        public double Altitude { get; set; }
        
        /// <summary>
        /// The speed in meters per second
        /// </summary>
        public double Speed { get; set; }
        
        /// <summary>
        /// The gimbal angle in degrees
        /// </summary>
        public double Angle { get; set; }
        
        /// <summary>
        /// The distance between waypoint lines in meters
        /// </summary>
        public double LineSpacing { get; set; }
        
        /// <summary>
        /// The starting index for waypoint numbering
        /// </summary>
        public int StartingIndex { get; set; }
        
        /// <summary>
        /// The action to perform at each waypoint (takePhoto, startRecord, etc.)
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Interval between photos (in seconds)
        /// </summary>
        public double PhotoInterval { get; set; }
        
        /// <summary>
        /// Whether to use only endpoints when generating waypoints
        /// If true, only the start and end points of a line will be used
        /// </summary>
        public bool UseEndpointsOnly { get; set; }
        
        /// <summary>
        /// Whether to use north-south pattern (true) or east-west pattern (false)
        /// </summary>
        public bool IsNorthSouth { get; set; }
        
        /// <summary>
        /// Unit type for measurements
        /// 0 = Metric, 1 = Imperial
        /// </summary>
        public int UnitType { get; set; }
        
        /// <summary>
        /// Camera focal length in millimeters
        /// </summary>
        public double FocalLength { get; set; }
        
        /// <summary>
        /// Camera sensor width in millimeters
        /// </summary>
        public double SensorWidth { get; set; }
        
        /// <summary>
        /// Camera sensor height in millimeters
        /// </summary>
        public double SensorHeight { get; set; }
        
        /// <summary>
        /// Overlap percentage between consecutive photos (0-100)
        /// </summary>
        public double Overlap { get; set; }
        
        /// <summary>
        /// Flag indicating whether the speed was manually set by the user
        /// If false, speed can be calculated from photo interval
        /// </summary>
        public bool ManualSpeedSet { get; set; }
    }
} 