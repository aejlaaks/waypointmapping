namespace KarttaBackEnd2.Server.Models
{
    public class KmlRequestModel
    {
        public string FlyToWaylineMode { get; set; }
        public string FinishAction { get; set; }
        public string ExitOnRCLost { get; set; }
        public string ExecuteRCLostAction { get; set; }
        public double GlobalTransitionalSpeed { get; set; }
        public DroneInfoModel DroneInfo { get; set; }
        public List<WaypointModel> Waypoints { get; set; }
        public List<ActionGroupModel> ActionGroups { get; set; }
    }
}
