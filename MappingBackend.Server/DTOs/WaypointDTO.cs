namespace KarttaBackEnd2.Server.DTOs
{
    public class WaypointDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public double GimbalAngle { get; set; }
        public string Action { get; set; }
    }
}
