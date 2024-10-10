
using KarttaBackEnd2.Server.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace KarttaBackEnd2.Server.DTOs
{
        public class GeneratePointsRequestDTO
        {
       

        public int UnitType { get; set; }
        public string? Action { get; set; }

        public double Altitude { get; set; }
            public double Speed { get; set; }
            public int Angle { get; set; }
            public double Distance { get; set; }
            public double Interval { get; set; }
            public List<Coordinate> ?Bounds { get; set; }
            public string BoundsType { get; set; }
            public int StartingIndex { get; set; }
            public double In_Distance { get; set; }
        [JsonProperty("allPointsAction")]
        public string? AllPointsAction { get; set; }
        public List<CoordinateCircle> Shapes { get; set; }
    }

      

    }
