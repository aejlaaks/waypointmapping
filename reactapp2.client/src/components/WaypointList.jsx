import React from 'react';
import PropTypes from 'prop-types';
import { WaypointActions } from './WaypointInfoBox';

// Convert action code to human-readable name
const getActionName = (actionCode) => {
  const actionMap = {
    [WaypointActions.NO_ACTION]: 'No Action',
    [WaypointActions.TAKE_PHOTO]: 'Take Photo',
    [WaypointActions.START_RECORD]: 'Start Recording',
    [WaypointActions.STOP_RECORD]: 'Stop Recording'
  };
  
  return actionMap[actionCode] || actionCode;
};

// Function to calculate row class based on waypoint type
const getRowClass = (waypoint, index, waypoints) => {
  const isVertex = waypoint.isVertex || 
    index === 0 || 
    index === waypoints.length - 1 || 
    (index > 0 && index < waypoints.length - 1 && hasSignificantHeadingChange(waypoints[index-1], waypoint, waypoints[index+1]));
  
  return isVertex ? 'table-warning' : '';
};

// Function to check if there's a significant heading change (to detect vertices)
const hasSignificantHeadingChange = (prev, current, next) => {
  if (!prev || !next) return false;
  
  // Simple check - if we had proper heading values we'd use those instead
  const angle1 = Math.atan2(current.lat - prev.lat, current.lng - prev.lng) * 180 / Math.PI;
  const angle2 = Math.atan2(next.lat - current.lat, next.lng - current.lng) * 180 / Math.PI;
  
  const change = Math.abs((angle2 - angle1 + 180) % 360 - 180);
  return change > 15; // Arbitrary threshold for heading change
};

const WaypointList = ({ waypoints, onUpdate, onDelete }) => {
  // Handle field updates
  const handleFieldChange = (id, field, value) => {
    onUpdate(id, { [field]: field === 'action' ? value : parseFloat(value) });
  };

  // Handle waypoint deletion
  const handleDelete = (id) => {
    onDelete(id);
  };

  return (
    <div className="waypoints-table mt-4">
      <h4>Waypoints</h4>
      {waypoints.length === 0 ? (
        <p className="text-muted">No waypoints available</p>
      ) : (
        <div className="table-responsive">
          <table className="table table-striped table-hover">
            <thead className="thead-light">
              <tr>
                <th>ID</th>
                <th>Lat</th>
                <th>Lng</th>
                <th>Alt (m)</th>
                <th>Speed (m/s)</th>
                <th>Heading</th>
                <th>G. Angle</th>
                <th>Action</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {waypoints.map((wp, index) => (
                <tr key={wp.id} className={getRowClass(wp, index, waypoints)}>
                  <td>{wp.id}</td>
                  <td>{wp.lat.toFixed(6)}</td>
                  <td>{wp.lng.toFixed(6)}</td>
                  <td>
                    <input
                      type="number"
                      className="form-control form-control-sm"
                      value={wp.altitude}
                      onChange={(e) => handleFieldChange(wp.id, 'altitude', e.target.value)}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      className="form-control form-control-sm"
                      value={wp.speed}
                      onChange={(e) => handleFieldChange(wp.id, 'speed', e.target.value)}
                    />
                  </td>
                  <td>
                    <div className="d-flex align-items-center">
                      <div 
                        className="waypoint-heading-indicator me-2" 
                        style={{ 
                          transform: `rotate(${wp.heading}deg)`,
                          fontSize: '14px' 
                        }}
                      >
                        ➤
                      </div>
                      <input
                        type="number"
                        className="form-control form-control-sm"
                        value={wp.heading}
                        onChange={(e) => handleFieldChange(wp.id, 'heading', e.target.value)}
                      />
                    </div>
                  </td>
                  <td>
                    <input
                      type="number"
                      className="form-control form-control-sm"
                      value={wp.angle}
                      onChange={(e) => handleFieldChange(wp.id, 'angle', e.target.value)}
                    />
                  </td>
                  <td>
                    <div className="d-flex align-items-center">
                      <select
                        value={wp.action}
                        className="form-select form-select-sm"
                        onChange={(e) => handleFieldChange(wp.id, 'action', e.target.value)}
                      >
                        <option value={WaypointActions.NO_ACTION}>No Action</option>
                        <option value={WaypointActions.TAKE_PHOTO}>Take Photo</option>
                        <option value={WaypointActions.START_RECORD}>Start Recording</option>
                        <option value={WaypointActions.STOP_RECORD}>Stop Recording</option>
                      </select>
                      <div className="ms-1">
                        {wp.action === WaypointActions.TAKE_PHOTO && <span>📷</span>}
                        {wp.action === WaypointActions.START_RECORD && <span>🎬</span>}
                        {wp.action === WaypointActions.STOP_RECORD && <span>⏹️</span>}
                      </div>
                    </div>
                  </td>
                  <td>
                    <div className="d-flex">
                      <button 
                        className="btn btn-danger btn-sm"
                        onClick={() => handleDelete(wp.id)}
                      >
                        <i className="bi bi-trash"></i>
                      </button>
                      <span 
                        className="badge ms-1" 
                        style={{
                          backgroundColor: index === 0 || index === waypoints.length - 1 ? '#FF4500' : 
                            getRowClass(wp, index, waypoints) ? '#FFC107' : '#3CB371'
                        }}
                        title={
                          index === 0 ? 'Start Point' : 
                          index === waypoints.length - 1 ? 'End Point' :
                          getRowClass(wp, index, waypoints) ? 'Vertex' : 'Intermediate'
                        }
                      >
                        {index === 0 ? 'S' : 
                         index === waypoints.length - 1 ? 'E' : 
                         getRowClass(wp, index, waypoints) ? 'V' : 'I'}
                      </span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

// Add PropTypes for better type checking
WaypointList.propTypes = {
  waypoints: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
      lat: PropTypes.number.isRequired,
      lng: PropTypes.number.isRequired,
      altitude: PropTypes.number.isRequired,
      speed: PropTypes.number.isRequired,
      angle: PropTypes.number.isRequired,
      heading: PropTypes.number.isRequired,
      action: PropTypes.string.isRequired
    })
  ).isRequired,
  onUpdate: PropTypes.func.isRequired,
  onDelete: PropTypes.func.isRequired
};

export default WaypointList;
