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
              {waypoints.map(wp => (
                <tr key={wp.id}>
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
                    <input
                      type="number"
                      className="form-control form-control-sm"
                      value={wp.heading}
                      onChange={(e) => handleFieldChange(wp.id, 'heading', e.target.value)}
                    />
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
                  </td>
                  <td>
                    <button 
                      className="btn btn-danger btn-sm"
                      onClick={() => handleDelete(wp.id)}
                    >
                      Delete
                    </button>
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
