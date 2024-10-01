namespace KarttaBackEnd2.Server.Models
{
    public class WaypointRequest
    {
        public int UnitType { get; set; }
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public int Angle { get; set; }
        public double Distance { get; set; }
        public List<(double Latitude, double Longitude)> ?Bounds { get; set; }
        public string ?BoundsType { get; set; }
        public int StartingIndex { get; set; }
    }
}
