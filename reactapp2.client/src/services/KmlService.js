/**
 * KmlService.js
 * Service for generating KML files from waypoints
 */

import { WaypointActions } from './WaypointService';

// Generate KML file content from waypoints
export const generateKml = (waypoints) => {
  if (!waypoints || waypoints.length === 0) {
    throw new Error('No waypoints provided for KML generation');
  }

  // XML header
  let kmlContent = '<?xml version="1.0" encoding="UTF-8"?>\n';
  kmlContent += '<kml xmlns="http://www.opengis.net/kml/2.2">\n';
  kmlContent += '  <Document>\n';
  kmlContent += '    <name>Drone Flight Path</name>\n';
  kmlContent += '    <description>Waypoints for drone flight</description>\n';
  
  // Style for waypoints
  kmlContent += '    <Style id="waypointStyle">\n';
  kmlContent += '      <IconStyle>\n';
  kmlContent += '        <Icon>\n';
  kmlContent += '          <href>http://maps.google.com/mapfiles/kml/paddle/red-circle.png</href>\n';
  kmlContent += '        </Icon>\n';
  kmlContent += '      </IconStyle>\n';
  kmlContent += '    </Style>\n';
  
  // Style for flight path
  kmlContent += '    <Style id="flightPathStyle">\n';
  kmlContent += '      <LineStyle>\n';
  kmlContent += '        <color>ff0000ff</color>\n';
  kmlContent += '        <width>4</width>\n';
  kmlContent += '      </LineStyle>\n';
  kmlContent += '    </Style>\n';
  
  // Flight path line
  kmlContent += '    <Placemark>\n';
  kmlContent += '      <name>Flight Path</name>\n';
  kmlContent += '      <styleUrl>#flightPathStyle</styleUrl>\n';
  kmlContent += '      <LineString>\n';
  kmlContent += '        <altitudeMode>absolute</altitudeMode>\n';
  kmlContent += '        <coordinates>\n';
  
  // Add coordinates for path
  waypoints.forEach(waypoint => {
    kmlContent += `          ${waypoint.lng},${waypoint.lat},${waypoint.altitude}\n`;
  });
  
  kmlContent += '        </coordinates>\n';
  kmlContent += '      </LineString>\n';
  kmlContent += '    </Placemark>\n';
  
  // Add each waypoint as a placemark
  waypoints.forEach((waypoint) => {
    kmlContent += '    <Placemark>\n';
    kmlContent += `      <name>Waypoint ${waypoint.id}</name>\n`;
    kmlContent += '      <styleUrl>#waypointStyle</styleUrl>\n';
    
    // Add description with waypoint details
    kmlContent += '      <description>\n';
    kmlContent += '        <![CDATA[\n';
    kmlContent += `          <p><strong>Altitude:</strong> ${waypoint.altitude} m</p>\n`;
    kmlContent += `          <p><strong>Speed:</strong> ${waypoint.speed} m/s</p>\n`;
    kmlContent += `          <p><strong>Heading:</strong> ${waypoint.heading} degrees</p>\n`;
    kmlContent += `          <p><strong>Gimbal Angle:</strong> ${waypoint.angle} degrees</p>\n`;
    kmlContent += `          <p><strong>Action:</strong> ${getActionName(waypoint.action)}</p>\n`;
    kmlContent += '        ]]>\n';
    kmlContent += '      </description>\n';
    
    // Add point geometry
    kmlContent += '      <Point>\n';
    kmlContent += '        <altitudeMode>absolute</altitudeMode>\n';
    kmlContent += `        <coordinates>${waypoint.lng},${waypoint.lat},${waypoint.altitude}</coordinates>\n`;
    kmlContent += '      </Point>\n';
    kmlContent += '    </Placemark>\n';
  });
  
  // Close document and kml tags
  kmlContent += '  </Document>\n';
  kmlContent += '</kml>';
  
  return kmlContent;
};

// Create a downloadable KML blob and URL
export const createKmlDownload = (waypoints) => {
  try {
    const kmlContent = generateKml(waypoints);
    const blob = new Blob([kmlContent], { type: 'application/vnd.google-earth.kml+xml' });
    return URL.createObjectURL(blob);
  } catch (error) {
    console.error('Error creating KML download:', error);
    return null;
  }
};

// Helper function to get human-readable action names
const getActionName = (actionCode) => {
  const actionMap = {
    [WaypointActions.NO_ACTION]: 'No Action',
    [WaypointActions.TAKE_PHOTO]: 'Take Photo',
    [WaypointActions.START_RECORD]: 'Start Recording',
    [WaypointActions.STOP_RECORD]: 'Stop Recording'
  };
  
  return actionMap[actionCode] || actionCode;
}; 