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

    return (
        <div className="waypoint-info-box p-3 bg-light border rounded">
            <h4 className="mb-3">Edit Waypoint {waypoint.id}</h4>
            <div className="mb-1 text-secondary">
                Location: {waypoint.lat.toFixed(6)}, {waypoint.lng.toFixed(6)}
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
