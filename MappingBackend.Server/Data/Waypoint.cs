using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Data
{
    public class Waypoint
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public double GimbalAngle { get; set; }
        public string Action { get; set; }
        public int FlightOrientation { get; set; } // 0 = East-West, 1 = North-South
        public bool FlipPath { get; set; } // Kääntää reitin
        public bool StraightenPaths { get; set; } // Suoristaa reitit
    }
}
