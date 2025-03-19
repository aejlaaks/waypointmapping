/**
 * MapService.js
 * Service for handling Google Maps specific operations
 */

// Calculate distance between two points in meters
export const calculateDistance = (lat1, lng1, lat2, lng2) => {
  const R = 6378.137; // Radius of earth in KM
  const dLat = lat2 * Math.PI / 180 - lat1 * Math.PI / 180;
  const dLng = lng2 * Math.PI / 180 - lng1 * Math.PI / 180;
  const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
    Math.sin(dLng / 2) * Math.sin(dLng / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  const d = R * c;
  return d * 1000; // Distance in meters
};

// Create a polyline on the map
export const createPolyline = (map, path, color = "#FF0000", weight = 2) => {
  const polyline = new google.maps.Polyline({
    path,
    geodesic: true,
    strokeColor: color,
    strokeOpacity: 1.0,
    strokeWeight: weight,
  });
  polyline.setMap(map);
  return polyline;
};

// Create a marker on the map
export const createMarker = (map, position, options = {}) => {
  const marker = new google.maps.Marker({
    position,
    map,
    ...options
  });
  return marker;
};

// Parse bounds string in the format "lat,lng; radius:xxx"
export const parseBoundsString = (boundsString) => {
  try {
    // Split coordinates and radius parts
    const [coordinatesPart, radiusPart] = boundsString.split("; radius:");
    const [lat, lng] = coordinatesPart.split(",").map(Number);
    const radius = parseFloat(radiusPart);

    // Check if coordinates and radius are valid numbers
    if (isNaN(lat) || isNaN(lng) || isNaN(radius)) {
      throw new Error("Invalid bounds string format");
    }

    // Return circle data as an object
    return { lat, lng, radius };
  } catch (error) {
    console.error("Error parsing bounds string:", error);
    return null;
  }
};

// Convert a coordinates string to an array of lat/lng objects
export const parseCoordinatesString = (coordinatesString) => {
  try {
    // Split the string by semicolon to get individual coordinate pairs
    const coordinatePairs = coordinatesString.split(';');

    // Map each pair to an object with lat and lng properties
    return coordinatePairs.map(pair => {
      const [lat, lng] = pair.split(',').map(Number);
      if (isNaN(lat) || isNaN(lng)) {
        throw new Error("Invalid coordinate values");
      }
      return { lat, lng };
    });
  } catch (error) {
    console.error("Error parsing coordinates string:", error);
    return [];
  }
}; 