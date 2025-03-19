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
    if (!boundsString || typeof boundsString !== 'string') {
      console.error("Invalid bounds string:", boundsString);
      return null;
    }
    
    console.log('Parsing bounds string:', boundsString);
    
    // Split coordinates and radius parts
    const parts = boundsString.split('; radius:');
    
    if (parts.length !== 2) {
      console.error('Invalid bounds string format - missing radius part:', boundsString);
      return null;
    }
    
    const coordinatesPart = parts[0].trim();
    const radiusPart = parts[1].trim();
    
    // Parse coordinates
    const [lat, lng] = coordinatesPart.split(',').map(val => {
      const cleaned = val.trim();
      const parsed = parseFloat(cleaned);
      if (isNaN(parsed)) {
        throw new Error(`Invalid coordinate value: ${cleaned}`);
      }
      return parsed;
    });
    
    // Parse radius
    const radius = parseFloat(radiusPart);
    
    // Check if coordinates and radius are valid numbers
    if (isNaN(lat) || isNaN(lng) || isNaN(radius)) {
      throw new Error("Invalid bounds string format - contains non-numeric values");
    }
    
    // Validate latitude and longitude ranges
    if (lat < -90 || lat > 90) {
      throw new Error(`Latitude out of range: ${lat}`);
    }
    
    if (lng < -180 || lng > 180) {
      throw new Error(`Longitude out of range: ${lng}`);
    }
    
    if (radius <= 0) {
      throw new Error(`Radius must be positive: ${radius}`);
    }

    console.log(`Parsed circle: Center(${lat}, ${lng}), Radius: ${radius}m`);
    
    // Special debug for circle coordinates
    if (lat === 0 && lng === 0) {
      console.warn('WARNING: Circle center at (0,0) detected! This is likely an error.');
      
      // If we have cached coordinates from the drawing manager, use those instead
      if (window.lastCircleCenter) {
        console.warn('Using cached circle center coordinates:', window.lastCircleCenter);
        return window.lastCircleCenter;
      }
    }
    
    // Return circle data as an object
    return { lat, lng, radius };
  } catch (error) {
    console.error("Error parsing bounds string:", error);
    throw error; // Re-throw to handle in calling code
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