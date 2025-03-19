import React, { useState } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css'; // Ensure Bootstrap is imported
import PropTypes from 'prop-types';

// Waypoint actions enum for consistency
export const WaypointActions = {
    NO_ACTION: 'noAction',
    TAKE_PHOTO: 'takePhoto',
    START_RECORD: 'startRecord',
    STOP_RECORD: 'stopRecord'
};

// Action options mapping for dropdown
const ACTION_OPTIONS = [
    { value: WaypointActions.NO_ACTION, label: 'No Action' },
    { value: WaypointActions.TAKE_PHOTO, label: 'Take Picture' },
    { value: WaypointActions.START_RECORD, label: 'Start Recording' },
    { value: WaypointActions.STOP_RECORD, label: 'Stop Recording' }
];

const WaypointInfoBox = ({ waypoint, onSave, onRemove }) => {
    const [formData, setFormData] = useState({
        altitude: waypoint.altitude,
        speed: waypoint.speed,
        angle: waypoint.angle,
        heading: waypoint.heading,
        action: waypoint.action
    });

    // Handle form input changes
    const handleChange = (e) => {
        const { id, value } = e.target;
        setFormData({
            ...formData,
            [id]: id === 'action' ? value : parseFloat(value)
        });
    };

    // Handle Save button click
    const handleSave = () => {
        onSave({ id: waypoint.id, ...formData });
    };

    // Handle Remove button click
    const handleRemove = () => {
        onRemove(waypoint.id);
    };

    // Add a function to calculate distance to the next waypoint
    const calculateDistanceToNextWaypoint = () => {
        // This would normally come from props or context
        // For now, we'll just indicate it's not available
        return 'N/A';
    };

    // Add a function to format lat/lng for better readability
    const formatCoordinate = (coord) => {
        return coord.toFixed(6);
    };

    return (
        <div className="waypoint-info-box p-3 bg-light border rounded">
            <h4 className="mb-3">Edit Waypoint {waypoint.id}</h4>
            <div className="mb-1 text-secondary">
                Location: {formatCoordinate(waypoint.lat)}, {formatCoordinate(waypoint.lng)}
            </div>

            {/* Add heading visualization */}
            <div className="d-flex align-items-center mb-3">
                <div 
                    className="waypoint-heading-indicator" 
                    style={{ transform: `rotate(${formData.heading}deg)` }}
                    title={`Heading: ${formData.heading}°`}
                >
                    ➤
                </div>
                <div className="ms-2">
                    <small className="text-muted">
                        Heading: {formData.heading}° | 
                        Distance to next: {calculateDistanceToNextWaypoint()}
                    </small>
                </div>
            </div>

            {/* Add a waypoint type indicator */}
            <div className="badge bg-info mb-3">
                {waypoint.isVertex ? 'Vertex Waypoint' : 'Intermediate Waypoint'}
            </div>

            <div className="form-group mb-3">
                <label htmlFor="altitude" className="form-label">Altitude (m)</label>
                <input
                    id="altitude"
                    type="number"
                    className="form-control"
                    value={formData.altitude}
                    onChange={handleChange}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="speed" className="form-label">Speed (m/s)</label>
                <input
                    id="speed"
                    type="number"
                    className="form-control"
                    value={formData.speed}
                    onChange={handleChange}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="angle" className="form-label">Gimbal Angle (degrees)</label>
                <input
                    id="angle"
                    type="number"
                    className="form-control"
                    value={formData.angle}
                    onChange={handleChange}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="heading" className="form-label">Heading (degrees North)</label>
                <input
                    id="heading"
                    type="number"
                    className="form-control"
                    value={formData.heading}
                    onChange={handleChange}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="action" className="form-label">Action</label>
                <div className="d-flex align-items-center">
                    <select
                        id="action"
                        className="form-select"
                        value={formData.action}
                        onChange={handleChange}
                    >
                        {ACTION_OPTIONS.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                    <div className="ms-2">
                        {formData.action === WaypointActions.TAKE_PHOTO && 
                            <span className="badge bg-primary">📷</span>}
                        {formData.action === WaypointActions.START_RECORD && 
                            <span className="badge bg-danger">🎬</span>}
                        {formData.action === WaypointActions.STOP_RECORD && 
                            <span className="badge bg-warning">⏹️</span>}
                    </div>
                </div>
            </div>

            <div className="d-flex justify-content-between mt-4">
                <button
                    onClick={handleSave}
                    className="btn btn-primary"
                >
                    Save
                </button>
                <button
                    onClick={handleRemove}
                    className="btn btn-danger"
                >
                    Remove
                </button>
            </div>
        </div>
    );
};

// Add PropTypes for better type checking
WaypointInfoBox.propTypes = {
    waypoint: PropTypes.shape({
        id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
        lat: PropTypes.number.isRequired,
        lng: PropTypes.number.isRequired,
        altitude: PropTypes.number.isRequired,
        speed: PropTypes.number.isRequired,
        angle: PropTypes.number.isRequired,
        heading: PropTypes.number.isRequired,
        action: PropTypes.string.isRequired
    }).isRequired,
    onSave: PropTypes.func.isRequired,
    onRemove: PropTypes.func.isRequired
};

export default WaypointInfoBox;
