using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace KarttaBackEnd2.Server.Models
{
    public class ActionGroupModel
    {
        public int ActionGroupId { get; set; }
        public int ActionGroupStartIndex { get; set; }
        public int ActionGroupEndIndex { get; set; }
        public string ActionGroupMode { get; set; }
        public string ActionTriggerType { get; set; }
        public ActionModel Action { get; set; }
    }
}
