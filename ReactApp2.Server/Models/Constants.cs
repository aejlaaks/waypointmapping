namespace KarttaBackEnd2.Server.Models
{
    /// <summary>
    /// Constants for waypoint actions
    /// </summary>
    public static class WaypointActions
    {
        public const string NoAction = "noAction";
        public const string TakePhoto = "takePhoto";
        public const string StartRecord = "startRecord";
        public const string StopRecord = "stopRecord";
    }

    /// <summary>
    /// Constants for shape types
    /// </summary>
    public static class ShapeTypes
    {
        public const string Rectangle = "rectangle";
        public const string Polygon = "polygon";
        public const string Circle = "circle";
        public const string Polyline = "polyline";
    }
} 