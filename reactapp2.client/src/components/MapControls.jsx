import React, { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import { calculateFlightParameters, WaypointActions } from '../services/WaypointService';
import 'bootstrap/dist/css/bootstrap.min.css';

const MapControls = ({ 
  onGenerateWaypoints, 
  onDownloadKml, 
  hasWaypoints 
}) => {
  // Control states
  const [altitude, setAltitude] = useState(60);
  const [speed, setSpeed] = useState(2.5);
  const [angle, setAngle] = useState(-45);
  const [focalLength, setFocalLength] = useState(24);
  const [sensorWidth, setSensorWidth] = useState(9.6);
  const [sensorHeight, setSensorHeight] = useState(7.2);
  const [photoInterval, setPhotoInterval] = useState(2);
  const [overlap, setOverlap] = useState(83);
  const [lineSpacing, setLineSpacing] = useState(10);
  const [useEndpointsOnly, setUseEndpointsOnly] = useState(true);
  const [isNorthSouth, setIsNorthSouth] = useState(true);
  const [startingIndex, setStartingIndex] = useState(1);
  const [allPointsAction, setAllPointsAction] = useState(WaypointActions.NO_ACTION);
  const [finalAction, setFinalAction] = useState('0');
  const [bounds, setBounds] = useState('');
  const [boundsType, setBoundsType] = useState('rectangle');
  const [shapeType, setShapeType] = useState('rectangle');

  // Automatically calculate line spacing and speed based on camera parameters
  useEffect(() => {
    const params = calculateFlightParameters({
      altitude,
      overlap,
      focalLength,
      sensorWidth,
      sensorHeight,
      interval: photoInterval
    });
    
    setLineSpacing(params.inDistance);
    setSpeed(params.speed);
  }, [altitude, overlap, focalLength, sensorWidth, sensorHeight, photoInterval]);

  // Handle form submission
  const handleSubmit = (e) => {
    e.preventDefault();
    
    onGenerateWaypoints({
      altitude,
      speed,
      angle,
      bounds,
      boundsType: shapeType,
      startingIndex,
      allPointsAction,
      finalAction,
      useEndpointsOnly,
      isNorthSouth,
      lineSpacing,
      overlap,
      photoInterval
    });
  };

  return (
    <div className="map-controls p-3 bg-light border rounded mb-3">
      <h4 className="mb-3">Flight Parameters</h4>
      
      <form onSubmit={handleSubmit}>
        <div className="row">
          {/* Camera Settings */}
          <div className="col-md-6">
            <h5 className="mb-2">Camera Settings</h5>
            <div className="form-group mb-2">
              <label className="form-label">Altitude (m)</label>
              <input 
                type="number" 
                className="form-control" 
                value={altitude} 
                onChange={(e) => setAltitude(parseFloat(e.target.value))} 
              />
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Gimbal Angle (degrees)</label>
              <input 
                type="number" 
                className="form-control" 
                value={angle} 
                onChange={(e) => setAngle(parseFloat(e.target.value))} 
              />
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Focal Length (mm)</label>
              <input 
                type="number" 
                className="form-control" 
                value={focalLength} 
                onChange={(e) => setFocalLength(parseFloat(e.target.value))} 
              />
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Sensor Width (mm)</label>
              <input 
                type="number" 
                className="form-control" 
                value={sensorWidth} 
                onChange={(e) => setSensorWidth(parseFloat(e.target.value))} 
              />
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Sensor Height (mm)</label>
              <input 
                type="number" 
                className="form-control" 
                value={sensorHeight} 
                onChange={(e) => setSensorHeight(parseFloat(e.target.value))} 
              />
            </div>
          </div>
          
          {/* Flight Settings */}
          <div className="col-md-6">
            <h5 className="mb-2">Flight Settings</h5>
            <div className="form-group mb-2">
              <label className="form-label">Photo Interval (s)</label>
              <input 
                type="number" 
                className="form-control" 
                value={photoInterval} 
                onChange={(e) => setPhotoInterval(parseFloat(e.target.value))} 
              />
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Overlap (%)</label>
              <input 
                type="number" 
                className="form-control" 
                value={overlap} 
                onChange={(e) => setOverlap(parseFloat(e.target.value))} 
              />
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Speed (m/s)</label>
              <input 
                type="number" 
                className="form-control" 
                value={speed} 
                readOnly 
              />
              <small className="form-text text-muted">Calculated automatically</small>
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Line Spacing (m)</label>
              <input 
                type="number" 
                className="form-control" 
                value={lineSpacing} 
                readOnly 
              />
              <small className="form-text text-muted">Calculated automatically</small>
            </div>
            
            <div className="form-group mb-2">
              <label className="form-label">Default Waypoint Action</label>
              <select 
                className="form-select" 
                value={allPointsAction} 
                onChange={(e) => setAllPointsAction(e.target.value)}
              >
                <option value={WaypointActions.NO_ACTION}>No Action</option>
                <option value={WaypointActions.TAKE_PHOTO}>Take Photo</option>
                <option value={WaypointActions.START_RECORD}>Start Recording</option>
                <option value={WaypointActions.STOP_RECORD}>Stop Recording</option>
              </select>
            </div>
          </div>
        </div>
        
        {/* Pattern Settings */}
        <div className="row mt-3">
          <div className="col-12">
            <h5 className="mb-2">Pattern Settings</h5>
            
            <div className="form-group mb-3">
              <label className="form-label">Shape Type</label>
              <select 
                className="form-select" 
                value={shapeType} 
                onChange={(e) => {
                  setShapeType(e.target.value);
                  setBoundsType(e.target.value);
                  // For polylines, default to using endpoints only
                  if (e.target.value === 'polyline') {
                    setUseEndpointsOnly(true);
                  }
                }}
              >
                <option value="rectangle">Rectangle</option>
                <option value="circle">Circle</option>
                <option value="polygon">Polygon</option>
                <option value="polyline">Polyline</option>
              </select>
            </div>
            
            <div className="d-flex mb-3">
              <div className="form-check me-3">
                <input 
                  type="checkbox" 
                  className="form-check-input" 
                  id="useEndpointsOnly" 
                  checked={useEndpointsOnly} 
                  onChange={() => setUseEndpointsOnly(!useEndpointsOnly)} 
                />
                <label className="form-check-label" htmlFor="useEndpointsOnly">
                  Use Endpoints Only
                  {shapeType === 'polyline' && 
                    <small className="d-block text-muted">Recommended for polylines</small>
                  }
                </label>
              </div>
              
              <div className="form-check">
                <input 
                  type="checkbox" 
                  className="form-check-input" 
                  id="isNorthSouth" 
                  checked={isNorthSouth} 
                  onChange={() => setIsNorthSouth(!isNorthSouth)} 
                  disabled={shapeType === 'polyline'} // Disable for polylines
                />
                <label className="form-check-label" htmlFor="isNorthSouth">
                  North-South Pattern
                  {shapeType === 'polyline' && 
                    <small className="d-block text-muted">Not applicable for polylines</small>
                  }
                </label>
              </div>
            </div>
            
            {shapeType === 'polyline' && (
              <div className="alert alert-info mb-3">
                <small>
                  <strong>Polyline Tips:</strong> For paths, waypoints will follow the polyline shape.
                  The "Use Endpoints Only" option creates waypoints only at the line vertices.
                  Disable to generate intermediate waypoints along line segments.
                </small>
              </div>
            )}
          </div>
        </div>
        
        {/* Action Buttons */}
        <div className="d-flex justify-content-between mt-4">
          <button type="submit" className="btn btn-primary">
            Generate Waypoints
          </button>
          
          <button 
            type="button" 
            className="btn btn-success" 
            disabled={!hasWaypoints} 
            onClick={onDownloadKml}
          >
            Download KML
          </button>
        </div>
      </form>
    </div>
  );
};

MapControls.propTypes = {
  onGenerateWaypoints: PropTypes.func.isRequired,
  onDownloadKml: PropTypes.func.isRequired,
  hasWaypoints: PropTypes.bool.isRequired
};

export default MapControls; 
