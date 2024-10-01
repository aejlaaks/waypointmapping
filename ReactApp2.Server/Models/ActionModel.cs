namespace KarttaBackEnd2.Server.Models
{
    public class ActionModel
    {
        public int ActionId { get; set; }
        public string ActionActuatorFunc { get; set; }
        public double GimbalPitchRotateAngle { get; set; }
        public int PayloadPositionIndex { get; set; }
    }
}
