/**
 * WaypointService.js
 * Service for managing waypoints and their operations
 */

import axios from 'axios';
const apiBaseUrl = (typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.VITE_API_BASE_URL) || '';

// Create API instance
const api = axios.create({
  baseURL: `${apiBaseUrl}/api/`,
});

// Waypoint actions enum for consistency
export const WaypointActions = {
  NO_ACTION: 'noAction',
  TAKE_PHOTO: 'takePhoto',
  START_RECORD: 'startRecord',
  STOP_RECORD: 'stopRecord'
};

// Generate waypoints on server
export const generateWaypoints = async (request) => {
  try {
    console.log('Original request for waypoints:', request);
    
    // Create a completely new object with only the properties we need
    // This avoids any circular references from Google Maps objects
    const cleanRequest = {
      Bounds: [],
      BoundsType: String(request.BoundsType || ''),
      Altitude: Number(request.altitude || 60),
      Speed: Number(request.speed || 2.5),
      Angle: Number(request.angle || -45),
      PhotoInterval: Number(request.photoInterval || 2),
      Overlap: Number(request.overlap || 80),
      LineSpacing: Number(request.inDistance || 10),
      // Use the standardized property name IsNorthSouth that matches the server-side model
      // Check both PascalCase and camelCase property names
      IsNorthSouth: request.IsNorthSouth === true || request.isNorthSouth === true,
      // Check both PascalCase and camelCase property names to avoid case sensitivity issues
      UseEndpointsOnly: request.UseEndpointsOnly === true || request.useEndpointsOnly === true,
      AllPointsAction: String(request.allPointsAction || 'noAction'),
      Action: String(request.allPointsAction || 'noAction'),
      FinalAction: String(request.finalAction || '0'),
      FlipPath: Boolean(request.flipPath),
      UnitType: Number(request.unitType || 0),
      StartingIndex: Number(request.startingIndex || 1)
    };
    
    // Safely copy bounds array, ensuring we only take the Lat and Lng properties
    if (request.Bounds && Array.isArray(request.Bounds)) {
      cleanRequest.Bounds = request.Bounds.map(coord => {
        if (coord && typeof coord === 'object') {
          return {
            Lat: Number(coord.Lat || 0),
            Lng: Number(coord.Lng || 0)
          };
        }
        return { Lat: 0, Lng: 0 };
      });
    }
    
    // Validate Bounds
    if (!cleanRequest.Bounds || !Array.isArray(cleanRequest.Bounds) || cleanRequest.Bounds.length === 0) {
      throw new Error('Invalid bounds data: Bounds must be a non-empty array');
    }
    
    // Check if Bounds contains the expected properties
    const hasValidBounds = cleanRequest.Bounds.every(coord => 
      typeof coord === 'object' && 
      typeof coord.Lat === 'number' && 
      typeof coord.Lng === 'number'
    );
    
    if (!hasValidBounds) {
      console.error('Invalid bounds format, expecting array of {Lat: number, Lng: number}:', cleanRequest.Bounds);
      throw new Error('Invalid bounds format: Each coordinate must have Lat and Lng properties');
    }
    
    // Check if BoundsType is valid
    if (!cleanRequest.BoundsType || typeof cleanRequest.BoundsType !== 'string') {
      throw new Error('Invalid boundsType: Must be a non-empty string');
    }

    console.log('Sending clean request to API:', cleanRequest);
    
    // Test that JSON serialization works before sending
    let serializedData;
    try {
      serializedData = JSON.stringify(cleanRequest);
      console.log('Serialized request data length:', serializedData.length);
    } catch (jsonError) {
      console.error('Request cannot be serialized to JSON:', jsonError);
      throw new Error('Request contains circular references that cannot be serialized');
    }
    
    // Log the useEndpointsOnly value before sending
    console.log('Original useEndpointsOnly in request:', request.useEndpointsOnly);
    console.log('Original UseEndpointsOnly in request:', request.UseEndpointsOnly);
    console.log('UseEndpointsOnly value in cleanRequest:', cleanRequest.UseEndpointsOnly);
    
    // Log the isNorthSouth value before sending
    console.log('Original isNorthSouth in request:', request.isNorthSouth);
    console.log('Original IsNorthSouth in request:', request.IsNorthSouth);
    console.log('IsNorthSouth value in cleanRequest:', cleanRequest.IsNorthSouth);
    
    // Use the clean request directly instead of re-parsing the serialized data
    const response = await api.post('/waypoints/generate', cleanRequest);
    console.log('API response:', response);
    console.log('API response data type:', typeof response.data);
    console.log('API response data length:', Array.isArray(response.data) ? response.data.length : 'not an array');
    console.log('API response data:', JSON.stringify(response.data, null, 2));
    
    if (Array.isArray(response.data) && response.data.length === 0) {
      console.warn('Server returned an empty array of waypoints');
    }
    
    return response.data;
  } catch (error) {
    console.error('Error in generateWaypoints:', error);
    if (error.response) {
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);
    }
    throw error.response?.data || error.message || 'Failed to generate waypoints';
  }
};

// Update waypoint on server
export const updateWaypoint = async (id, updatedWaypoint) => {
  try {
    const response = await api.put(`/waypoints/${id}`, updatedWaypoint);
    return response.data;
  } catch (error) {
    throw error.response?.data || 'Failed to update waypoint';
  }
};

// Delete waypoint from server
export const deleteWaypoint = async (id) => {
  try {
    const response = await api.delete(`/waypoints/${id}`);
    return response.data;
  } catch (error) {
    throw error.response?.data || 'Failed to delete waypoint';
  }
};

// Calculate flight parameters based on camera settings
export const calculateFlightParameters = ({
  altitude,
  overlap,
  focalLength,
  sensorWidth,
  sensorHeight,
  interval
}) => {
  // Convert all parameters to numbers
  const altitudeNum = parseFloat(altitude);
  const overlapNum = parseFloat(overlap);
  const focalLengthNum = parseFloat(focalLength);
  const sensorWidthNum = parseFloat(sensorWidth);
  const sensorHeightNum = parseFloat(sensorHeight);
  const intervalNum = parseFloat(interval);

  // Calculate horizontal and vertical FOV in radians
  const fovH = 2 * Math.atan(sensorWidthNum / (2 * focalLengthNum));
  const fovV = 2 * Math.atan(sensorHeightNum / (2 * focalLengthNum));

  // Calculate ground coverage dimensions
  const groundWidth = 2 * altitudeNum * Math.tan(fovH / 2);
  const groundHeight = 2 * altitudeNum * Math.tan(fovV / 2);

  // Calculate distance between flight lines
  const inDistance = groundWidth * (1 - overlapNum / 100);
  
  // Calculate distance between photos
  const distanceBetweenPhotos = groundHeight * (1 - overlapNum / 100);
  
  // Calculate required speed
  const speed = distanceBetweenPhotos / intervalNum;

  return {
    inDistance: inDistance.toFixed(1),
    speed: speed.toFixed(1),
    groundWidth: groundWidth.toFixed(1),
    groundHeight: groundHeight.toFixed(1)
  };
};

// Generate a basic waypoint model for new waypoints
export const createWaypointModel = (id, lat, lng, options = {}) => {
  return {
    id: id,
    lat: lat,
    lng: lng,
    altitude: options.altitude || 60,
    speed: options.speed || 2.5,
    angle: options.angle || -45,
    heading: options.heading || 0,
    action: options.action || WaypointActions.NO_ACTION,
    ...options
  };
}; 