namespace KarttaBackEnd2.Server.Models
{
    public class WaypointGen
    {
        public int Index { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double ExecuteHeight { get; set; }
        public double WaypointSpeed { get; set; }
        public string WaypointHeadingMode { get; set; }
        public string WaypointHeadingPathMode { get; set; }
        public string WaypointTurnMode { get; set; }
        public string WaypointTurnDampingDist { get; set; }
        public string Action { get; set; }
        public object? WaypointHeadingAngle { get; internal set; }
        public string? WaypointPoiPoint { get; internal set; }
        public object ?WaypointHeadingAngleEnable { get; internal set; }
        public object ?UseStraightLine { get; internal set; }
        public object ?ActionGroupId { get; internal set; }
        public object ?ActionGroupStartIndex { get; internal set; }
        public object ?ActionGroupEndIndex { get; internal set; }
        public string? ActionGroupMode { get; internal set; }
        public string? ActionTriggerType { get; internal set; }
        public object ?ActionId { get; internal set; }
        public string? ActionActuatorFunc { get; internal set; }
        public object ?GimbalPitchRotateAngle { get; internal set; }
        public object ?PayloadPositionIndex { get; internal set; }
    }
}
