using System.IO;

namespace KarttaBackEnd2.Server.Models
{

    public class FlyToWaylineRequest
    {
        public string ?FlyToWaylineMode { get; set; }
        public string ?FinishAction { get; set; }
        public string ?ExitOnRCLost { get; set; }
        public string ?ExecuteRCLostAction { get; set; }
        public double GlobalTransitionalSpeed { get; set; }
        public DroneInfo ? DroneInfo { get; set; }
        public List<WaypointGen> Waypoints { get; set; }
        public bool useEndpointsOnly { get; set; }
        public int Interval { get; set; }

    }

    public class DroneInfo
    {
        public int DroneEnumValue { get; set; }
        public int DroneSubEnumValue { get; set; }
    }
}
