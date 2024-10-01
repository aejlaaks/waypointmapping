using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace KarttaBackEnd2.Server.Services
{
    public class KMZService : IKMZService
    {
        private readonly string _templateKmlPath = "/mnt/data/template.kml";
        private readonly string _waylinesWpmlPath = "/mnt/data/waylines.wpml";

        public async Task<byte[]> GenerateKmzAsync(FlyToWaylineRequest request)
        {
            // If required parameters are missing, use default values from the template and waylines files
            await SetDefaultValuesAsync(request);

            // Generate KML
            string kmlContent = await GenerateKmlAsync(request);

            // Generate WPML
            string wpmlContent = await GenerateWpmlAsync(request);

            // Create KMZ (ZIP) file
            return await CreateKmzAsync(kmlContent, wpmlContent);
        }

        private async Task SetDefaultValuesAsync(FlyToWaylineRequest request)
        {
            if (request.Waypoints == null || !request.Waypoints.Any())
            {
                request.Waypoints = await GetDefaultWaypointsAsync();
            }

            if (string.IsNullOrEmpty(request.FlyToWaylineMode))
            {
                request.FlyToWaylineMode = "safely"; // Default value from template file
            }

            if (string.IsNullOrEmpty(request.FinishAction))
            {
                request.FinishAction = "noAction"; // Default value from template file
            }

            if (string.IsNullOrEmpty(request.ExitOnRCLost))
            {
                request.ExitOnRCLost = "executeLostAction"; // Default value from template file
            }

            if (string.IsNullOrEmpty(request.ExecuteRCLostAction))
            {
                request.ExecuteRCLostAction = "hover"; // Default value from template file
            }

            if (request.GlobalTransitionalSpeed <= 0)
            {
                request.GlobalTransitionalSpeed = 2.5; // Default value from template file
            }

            if (request.DroneInfo == null)
            {
                request.DroneInfo = new DroneInfo
                {
                    DroneEnumValue = 68, // Default from template file
                    DroneSubEnumValue = 0 // Default from template file
                };
            }
        }

        private async Task<List<WaypointGen>> GetDefaultWaypointsAsync()
        {
            var waypoints = new List<WaypointGen>();
            var lines = await File.ReadAllLinesAsync(_waylinesWpmlPath);
            foreach (var line in lines)
            {
                if (line.Contains("<Waypoint"))
                {
                    var waypoint = new WaypointGen
                    {
                        Index = GetValueFromTag(line, "Index"),
                        Latitude = GetValueFromTag(line, "Lat"),
                        Longitude = GetValueFromTag(line, "Lng"),
                        ExecuteHeight = GetValueFromTag(line, "Height"),
                        WaypointSpeed = GetValueFromTag(line, "Speed")
                    };
                    waypoints.Add(waypoint);
                }
            }
            return waypoints;
        }

        private async Task<string> GenerateKmlAsync(FlyToWaylineRequest request)
        {
            // Get current Unix timestamp in milliseconds for createTime and updateTime
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Build the KML content manually with the exact structure you need
            var kmlContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <kml xmlns=""http://www.opengis.net/kml/2.2"" xmlns:wpml=""http://www.dji.com/wpmz/1.0.2"">
                  <Document>
                    <wpml:author>fly</wpml:author>
                    <wpml:createTime>{timestamp}</wpml:createTime>
                    <wpml:updateTime>{timestamp}</wpml:updateTime>
                    <wpml:missionConfig>
                      <wpml:flyToWaylineMode>{request.FlyToWaylineMode ?? "safely"}</wpml:flyToWaylineMode>
                      <wpml:finishAction>{request.FinishAction ?? "goHome"}</wpml:finishAction>
                      <wpml:exitOnRCLost>{request.ExitOnRCLost ?? "executeLostAction"}</wpml:exitOnRCLost>
                      <wpml:executeRCLostAction>{request.ExecuteRCLostAction ?? "goBack"}</wpml:executeRCLostAction>
                      <wpml:globalTransitionalSpeed>{request.GlobalTransitionalSpeed.ToString(CultureInfo.InvariantCulture) ?? "2.5"}</wpml:globalTransitionalSpeed>
                      <wpml:droneInfo>
                        <wpml:droneEnumValue>{request.DroneInfo?.DroneEnumValue.ToString(CultureInfo.InvariantCulture) ?? "68"}</wpml:droneEnumValue>
                        <wpml:droneSubEnumValue>{request.DroneInfo?.DroneSubEnumValue.ToString(CultureInfo.InvariantCulture) ?? "0"}</wpml:droneSubEnumValue>
                      </wpml:droneInfo>
                    </wpml:missionConfig>
                  </Document>
                </kml>";

            return await Task.FromResult(kmlContent);
        }

        private async Task<string> GenerateWpmlAsync(FlyToWaylineRequest request)
        {
            var wpmlContent = new System.Text.StringBuilder();

            // Start with the XML declaration and the KML opening tags including wpml:missionConfig
            wpmlContent.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            wpmlContent.AppendLine(@"<kml xmlns=""http://www.opengis.net/kml/2.2"" xmlns:wpml=""http://www.dji.com/wpmz/1.0.2"">");
            wpmlContent.AppendLine(@"  <Document>");
            wpmlContent.AppendLine(@"    <wpml:missionConfig>");
            wpmlContent.AppendLine($@"      <wpml:flyToWaylineMode>{request.FlyToWaylineMode ?? "safely"}</wpml:flyToWaylineMode>");
            wpmlContent.AppendLine($@"      <wpml:finishAction>{request.FinishAction ?? "goHome"}</wpml:finishAction>");
            wpmlContent.AppendLine($@"      <wpml:exitOnRCLost>{request.ExitOnRCLost ?? "executeLostAction"}</wpml:exitOnRCLost>");
            wpmlContent.AppendLine($@"      <wpml:executeRCLostAction>{request.ExecuteRCLostAction ?? "goBack"}</wpml:executeRCLostAction>");
            wpmlContent.AppendLine($@"      <wpml:globalTransitionalSpeed>{request.GlobalTransitionalSpeed.ToString(CultureInfo.InvariantCulture) ?? "2.5"}</wpml:globalTransitionalSpeed>");
            wpmlContent.AppendLine(@"      <wpml:droneInfo>");
            wpmlContent.AppendLine($@"        <wpml:droneEnumValue>{request.DroneInfo?.DroneEnumValue.ToString(CultureInfo.InvariantCulture) ?? "68"}</wpml:droneEnumValue>");
            wpmlContent.AppendLine($@"        <wpml:droneSubEnumValue>{request.DroneInfo?.DroneSubEnumValue.ToString(CultureInfo.InvariantCulture) ?? "0"}</wpml:droneSubEnumValue>");
            wpmlContent.AppendLine(@"      </wpml:droneInfo>");
            wpmlContent.AppendLine(@"    </wpml:missionConfig>");

            // Start the Folder section
            wpmlContent.AppendLine(@"    <Folder>");
            wpmlContent.AppendLine(@"      <wpml:templateId>0</wpml:templateId>");
            wpmlContent.AppendLine(@"      <wpml:executeHeightMode>relativeToStartPoint</wpml:executeHeightMode>");
            wpmlContent.AppendLine(@"      <wpml:waylineId>0</wpml:waylineId>");
            wpmlContent.AppendLine(@"      <wpml:distance>0</wpml:distance>");
            wpmlContent.AppendLine(@"      <wpml:duration>0</wpml:duration>");
            wpmlContent.AppendLine(@"      <wpml:autoFlightSpeed>2.5</wpml:autoFlightSpeed>");

            // Add the waypoints inside Placemark
            //wpmlContent.AppendLine(@"      <Placemark>");

            foreach (var waypoint in request.Waypoints)
            {
                var lat = waypoint.Latitude.ToString("F14", CultureInfo.InvariantCulture);
                var lng = waypoint.Longitude.ToString("F14", CultureInfo.InvariantCulture);
                var height = waypoint.ExecuteHeight.ToString(CultureInfo.InvariantCulture);
                var speed = waypoint.WaypointSpeed.ToString(CultureInfo.InvariantCulture);
                var headingMode = waypoint.WaypointHeadingMode ?? "followWayline";
                var headingAngle = waypoint.WaypointHeadingAngle ?? "0";
                var waypointPoiPoint = waypoint.WaypointPoiPoint ?? "0.000000,0.000000,0.000000";
                var headingAngleEnable = waypoint.WaypointHeadingAngleEnable ?? "0";
                var pathMode = waypoint.WaypointHeadingPathMode ?? "followBadArc";
                var turnMode = waypoint.WaypointTurnMode ?? "toPointAndPassWithContinuityCurvature";
                var dampingDist = waypoint.WaypointTurnDampingDist.ToString(CultureInfo.InvariantCulture) ?? "0";
                var useStraightLine = waypoint.UseStraightLine ?? "0";
                var actionGroupId = waypoint.ActionGroupId ?? "2";
                var actionStartIndex = waypoint.ActionGroupStartIndex ?? waypoint.Index.ToString();
                var actionEndIndex = waypoint.ActionGroupEndIndex ?? waypoint.Index.ToString();
                var actionGroupMode = waypoint.ActionGroupMode ?? "parallel";
                var actionTriggerType = waypoint.ActionTriggerType ?? "reachPoint";
                var actionId = waypoint.ActionId ?? "206";
                var actuatorFunc = waypoint.ActionActuatorFunc ?? "takePhoto";
                var actuatorPitchAngle = waypoint.GimbalPitchRotateAngle ?? "-45";
                var payloadPositionIndex = waypoint.PayloadPositionIndex ?? "0";

                wpmlContent.AppendLine($@"
                    <Placemark>
                        <Point>
                            <coordinates>{lng},{lat}</coordinates>
                        </Point>
                        <wpml:index>{waypoint.Index}</wpml:index>
                        <wpml:executeHeight>{height}</wpml:executeHeight>
                        <wpml:waypointSpeed>{speed}</wpml:waypointSpeed>
                        <wpml:waypointHeadingParam>
                            <wpml:waypointHeadingMode>{headingMode}</wpml:waypointHeadingMode>
                            <wpml:waypointHeadingAngle>{headingAngle}</wpml:waypointHeadingAngle>
                            <wpml:waypointPoiPoint>{waypointPoiPoint}</wpml:waypointPoiPoint>
                            <wpml:waypointHeadingAngleEnable>{headingAngleEnable}</wpml:waypointHeadingAngleEnable>
                            <wpml:waypointHeadingPathMode>{pathMode}</wpml:waypointHeadingPathMode>
                        </wpml:waypointHeadingParam>
                        <wpml:waypointTurnParam>
                            <wpml:waypointTurnMode>{turnMode}</wpml:waypointTurnMode>
                            <wpml:waypointTurnDampingDist>{dampingDist}</wpml:waypointTurnDampingDist>
                        </wpml:waypointTurnParam>
                        <wpml:useStraightLine>{useStraightLine}</wpml:useStraightLine>
                        <wpml:actionGroup>
                            <wpml:actionGroupId>{actionGroupId}</wpml:actionGroupId>
                            <wpml:actionGroupStartIndex>{actionStartIndex}</wpml:actionGroupStartIndex>
                            <wpml:actionGroupEndIndex>{actionEndIndex}</wpml:actionGroupEndIndex>
                            <wpml:actionGroupMode>{actionGroupMode}</wpml:actionGroupMode>
                            <wpml:actionTrigger>
                                <wpml:actionTriggerType>{actionTriggerType}</wpml:actionTriggerType>
                            </wpml:actionTrigger>
                            <wpml:action>
                                <wpml:actionId>{actionId}</wpml:actionId>
                                <wpml:actionActuatorFunc>{actuatorFunc}</wpml:actionActuatorFunc>
                                <wpml:actionActuatorFuncParam>
                                    <wpml:gimbalPitchRotateAngle>{actuatorPitchAngle}</wpml:gimbalPitchRotateAngle>
                                    <wpml:payloadPositionIndex>{payloadPositionIndex}</wpml:payloadPositionIndex>
                                </wpml:actionActuatorFuncParam>
                            </wpml:action>
                        </wpml:actionGroup>
                    </Placemark>");
            }

            // End the closing tags
            wpmlContent.AppendLine(@"    </Folder>");
            wpmlContent.AppendLine(@"  </Document>");
            wpmlContent.AppendLine(@"</kml>");

            return await Task.FromResult(wpmlContent.ToString());
        }

        private async Task<byte[]> CreateKmzAsync(string kmlContent, string wpmlContent)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Create the folder 'wpmz' in the zip archive
                    var kmlFolderPath = "wpmz/";

                    // Add KML file to archive
                    var kmlEntry = zipArchive.CreateEntry($"{kmlFolderPath}template.kml");
                    using (var kmlStream = kmlEntry.Open())
                    using (var streamWriter = new StreamWriter(kmlStream))
                    {
                        await streamWriter.WriteAsync(kmlContent);
                    }

                    // Add WPML file to archive
                    var wpmlEntry = zipArchive.CreateEntry($"{kmlFolderPath}waylines.wpml");
                    using (var wpmlStream = wpmlEntry.Open())
                    using (var streamWriter = new StreamWriter(wpmlStream))
                    {
                        await streamWriter.WriteAsync(wpmlContent);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        private int GetValueFromTag(string line, string tagName)
        {
            var startTag = $"{tagName}=\"";
            var startIndex = line.IndexOf(startTag) + startTag.Length;
            var endIndex = line.IndexOf("\"", startIndex);
            return int.Parse(line.Substring(startIndex, endIndex - startIndex));
        }
    }
}
