import { useState, useEffect } from 'react';
import { calculateFlightParameters } from '../services/WaypointService';

/**
 * Custom hook to manage flight parameters and calculations
 */
export const useFlightParameters = (initialState = {}) => {
  // State for camera and flight parameters
  const [altitude, setAltitude] = useState(initialState.altitude || 60);
  const [speed, setSpeed] = useState(initialState.speed || 2.5);
  const [angle, setAngle] = useState(initialState.angle || -45);
  const [focalLength, setFocalLength] = useState(initialState.focalLength || 24);
  const [sensorWidth, setSensorWidth] = useState(initialState.sensorWidth || 9.6);
  const [sensorHeight, setSensorHeight] = useState(initialState.sensorHeight || 7.2);
  const [photoInterval, setPhotoInterval] = useState(initialState.photoInterval || 2);
  const [overlap, setOverlap] = useState(initialState.overlap || 83);
  const [inDistance, setInDistance] = useState(initialState.inDistance || 10);
  const [isNorthSouth, setIsNorthSouth] = useState(() => {
    console.log('Initializing isNorthSouth with initialState:', initialState.isNorthSouth);
    // Default to true if not explicitly false
    return initialState.isNorthSouth === false ? false : true;
  });
  const [useEndpointsOnly, setUseEndpointsOnly] = useState(() => {
    console.log('Initializing useEndpointsOnly with initialState:', initialState.useEndpointsOnly);
    // Default to false if not explicitly true
    return initialState.useEndpointsOnly === true ? true : false;
  });
  const [allPointsAction, setAllPointsAction] = useState(initialState.allPointsAction || 'takePhoto');
  const [finalAction, setFinalAction] = useState(initialState.finalAction || '0');
  const [flipPath, setFlipPath] = useState(initialState.flipPath || false);
  const [unitType, setUnitType] = useState(initialState.unitType || '0');

  // Calculate flight parameters whenever relevant state changes
  useEffect(() => {
    const params = calculateFlightParameters({
      altitude,
      overlap,
      focalLength,
      sensorWidth,
      sensorHeight,
      interval: photoInterval
    });
    
    setInDistance(params.inDistance);
    
    // Only update speed if the user hasn't manually changed it
    if (!initialState.manualSpeedSet) {
      setSpeed(params.speed);
    }
  }, [altitude, overlap, focalLength, sensorWidth, sensorHeight, photoInterval]);

  // Explicitly define setUseEndpointsOnly to ensure boolean values
  const setUseEndpointsOnlyWithBool = (value) => {
    // Always convert to explicit boolean
    const boolValue = value === true;
    console.log(`setUseEndpointsOnly called with ${value} (${typeof value}), setting to ${boolValue}`);
    setUseEndpointsOnly(boolValue);
  };
  
  // Explicitly define setIsNorthSouth to ensure boolean values
  const setIsNorthSouthWithBool = (value) => {
    // Always convert to explicit boolean
    const boolValue = value === true;
    console.log(`setIsNorthSouth called with ${value} (${typeof value}), setting to ${boolValue}`);
    setIsNorthSouth(boolValue);
  };
  
  // Toggle functions
  const toggleUseEndpointsOnly = () => {
    // Force the value to be a boolean with the !! operator
    const currentValue = !!useEndpointsOnly;
    const newValue = !currentValue;
    console.log(`toggleUseEndpointsOnly: changing from ${currentValue} (${typeof useEndpointsOnly}) to ${newValue}`);
    
    // Directly set the new value instead of using a callback
    setUseEndpointsOnly(newValue);
    
    // Immediately log the new state value (though it won't reflect in state until next render)
    console.log('toggleUseEndpointsOnly: set new value to', newValue, 'type:', typeof newValue);
  };
  
  const toggleIsNorthSouth = () => {
    // Force the value to be a boolean with the !! operator
    const currentValue = !!isNorthSouth;
    const newValue = !currentValue;
    console.log(`toggleIsNorthSouth: changing from ${currentValue} (${typeof isNorthSouth}) to ${newValue}`);
    
    // Directly set the new value instead of using a callback
    setIsNorthSouth(newValue);
    
    // Immediately log the new state value (though it won't reflect in state until next render)
    console.log('toggleIsNorthSouth: set new value to', newValue, 'type:', typeof newValue);
  };
  const toggleFlipPath = () => setFlipPath(prev => !prev);

  // Package all states and setters into an object for easy access
  return {
    // State variables
    altitude,
    speed,
    angle,
    focalLength,
    sensorWidth,
    sensorHeight,
    photoInterval,
    overlap,
    inDistance,
    isNorthSouth,
    useEndpointsOnly,
    allPointsAction,
    finalAction,
    flipPath,
    unitType,
    
    // Setters
    setAltitude,
    setSpeed,
    setAngle,
    setFocalLength,
    setSensorWidth,
    setSensorHeight,
    setPhotoInterval,
    setOverlap,
    setInDistance,
    setIsNorthSouth: setIsNorthSouthWithBool,
    setUseEndpointsOnly: setUseEndpointsOnlyWithBool,
    setAllPointsAction,
    setFinalAction,
    setUnitType,
    
    // Toggle functions
    toggleUseEndpointsOnly,
    toggleIsNorthSouth,
    toggleFlipPath,
    
    // Get all parameters as an object (for API calls)
    getFlightParameters: () => {
      // Create the parameters object with explicit boolean handling
      const params = {
        altitude,
        speed,
        angle,
        interval: photoInterval,
        overlap,
        inDistance,
        isNorthSouth: isNorthSouth === true,
        useEndpointsOnly: useEndpointsOnly === true,
        allPointsAction,
        finalAction,
        flipPath: flipPath === true,
        unitType
      };
      
      console.log('getFlightParameters returning with useEndpointsOnly:', params.useEndpointsOnly);
      return params;
    }
  };
}; 