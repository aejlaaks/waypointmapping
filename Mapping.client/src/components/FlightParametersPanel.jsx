import React, { useRef, useEffect } from 'react';
import '../App.css';

/**
 * Component for displaying and editing flight parameters
 */
const FlightParametersPanel = ({ 
  flightParameters,
  onValueChange
}) => {
  const {
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
    toggleUseEndpointsOnly,
    toggleIsNorthSouth,
    setUseEndpointsOnly,
    setIsNorthSouth
  } = flightParameters;
  
  const checkboxRef = useRef(null);
  const northSouthCheckboxRef = useRef(null);
  
  // Direct DOM manipulation to force checkbox state
  const handleEndpointsOnlyChange = (e) => {
    const newValue = e.target.checked;
    console.log(`Direct checkbox change to: ${newValue} (${typeof newValue})`);
    
    // Force the checkbox checked state via DOM - do this first
    if (checkboxRef.current) {
      checkboxRef.current.checked = newValue;
    }
    
    // Then update the React state with the explicit boolean value
    setUseEndpointsOnly(newValue === true);
    
    console.log('After setting, checkbox.checked =', checkboxRef.current?.checked);
  };
  
  // Direct DOM manipulation to force North-South checkbox state
  const handleNorthSouthChange = (e) => {
    const newValue = e.target.checked;
    console.log(`North-South checkbox change to: ${newValue} (${typeof newValue})`);
    
    // Force the checkbox checked state via DOM - do this first
    if (northSouthCheckboxRef.current) {
      northSouthCheckboxRef.current.checked = newValue;
    }
    
    // Then update the React state with the explicit boolean value
    setIsNorthSouth(newValue === true);
    
    console.log('After setting, N-S checkbox.checked =', northSouthCheckboxRef.current?.checked);
  };

  return (
    <div className="input-container">
      <h3 className="section-header">Flight Parameters</h3>
      <label>
        Altitude (m)
        <input
          type="number"
          placeholder="Altitude"
          value={altitude}
          onChange={(e) => onValueChange('altitude', e.target.value)}
          className="input-style"
        />
      </label>
      <label>
        Speed (m/s)
        <input
          type="number"
          placeholder="Speed"
          value={speed}
          onChange={(e) => onValueChange('speed', e.target.value)}
          className="input-style"
        />
      </label>
      <label>
        Gimbal Angle (°)
        <input
          type="number"
          placeholder="Angle"
          value={angle}
          onChange={(e) => onValueChange('angle', e.target.value)}
          className="input-style"
        />
      </label>

      <h3 className="section-header">Camera Settings</h3>
      <label>
        Focal Length (mm)
        <input
          type="number"
          placeholder="Focal Length"
          value={focalLength}
          onChange={(e) => onValueChange('focalLength', e.target.value)}
          className="input-style"
        />
      </label>
      <label>
        Sensor Width (mm)
        <input
          type="number"
          placeholder="Sensor Width"
          value={sensorWidth}
          onChange={(e) => onValueChange('sensorWidth', e.target.value)}
          className="input-style"
        />
      </label>
      <label>
        Sensor Height (mm)
        <input
          type="number"
          placeholder="Sensor Height"
          value={sensorHeight}
          onChange={(e) => onValueChange('sensorHeight', e.target.value)}
          className="input-style"
        />
      </label>

      <h3 className="section-header">Path Options</h3>
      <label>
        North-South Direction
        <input
          type="checkbox"
          ref={northSouthCheckboxRef}
          checked={isNorthSouth}
          onChange={handleNorthSouthChange}
          className="input-style"
        />
      </label>
      <label>
        Use Endpoints Only
        <input
          type="checkbox"
          ref={checkboxRef}
          checked={useEndpointsOnly}
          onChange={handleEndpointsOnlyChange}
          className="input-style"
        />
      </label>

      <h3 className="section-header">Photo Settings</h3>
      <label>
        Photo Interval (s)
        <input
          type="number"
          placeholder="Photo Interval"
          value={photoInterval}
          onChange={(e) => onValueChange('photoInterval', e.target.value)}
          className="input-style"
        />
      </label>
      <label>
        Overlap (%)
        <input
          type="number"
          placeholder="Overlap"
          value={overlap}
          onChange={(e) => onValueChange('overlap', e.target.value)}
          className="input-style"
        />
      </label>
      <label>
        Line Spacing (m)
        <input
          type="number"
          placeholder="Line Spacing"
          value={inDistance}
          onChange={(e) => onValueChange('inDistance', e.target.value)}
          className="input-style"
        />
      </label>
      <label>
        Waypoint Action
        <select
          id="in_allPointsAction"
          value={allPointsAction}
          onChange={(e) => onValueChange('allPointsAction', e.target.value)}
          className="input-style"
        >
          <option value="noAction">No Action</option>
          <option value="takePhoto">Take Photo</option>
          <option value="startRecord">Start Recording</option>
          <option value="stopRecord">Stop Recording</option>
        </select>
      </label>
    </div>
  );
};

export default FlightParametersPanel; 