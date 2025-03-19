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
    const response = await api.post('/waypoints/generatePoints', request);
    return response.data;
  } catch (error) {
    throw error.response?.data || 'Failed to generate waypoints';
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