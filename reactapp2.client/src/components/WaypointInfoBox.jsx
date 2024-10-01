import React, { useState } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css'; // Ensure Bootstrap is imported

const WaypointInfoBox = ({ waypoint, onSubmit, onSave, onRemove }) => {
    const [altitude, setAltitude] = useState(waypoint.altitude);
    const [speed, setSpeed] = useState(waypoint.speed);
    const [angle, setAngle] = useState(waypoint.angle);
    const [heading, setHeading] = useState(waypoint.heading);
    const [action, setAction] = useState(waypoint.action);

    // This will trigger when the Submit button is clicked
    const handleSubmit = () => {
        const updatedWaypoint = {
            ...waypoint,
            altitude,
            speed,
            angle,
            heading,
            action,
        };
        onSubmit(updatedWaypoint); // Call the submit function passed from the parent
    };

    return (
        <div className="waypoint-info-box p-3 bg-light border rounded">
            <h2 className="mb-4">Edit Waypoint {waypoint.id}</h2>

            <div className="form-group mb-3">
                <label htmlFor="altitude">Altitude</label>
                <input
                    id="altitude"
                    type="number"
                    className="form-control"
                    value={altitude}
                    onChange={(e) => setAltitude(parseFloat(e.target.value))}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="speed">Speed</label>
                <input
                    id="speed"
                    type="number"
                    className="form-control"
                    value={speed}
                    onChange={(e) => setSpeed(parseFloat(e.target.value))}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="angle">Angle</label>
                <input
                    id="angle"
                    type="number"
                    className="form-control"
                    value={angle}
                    onChange={(e) => setAngle(parseFloat(e.target.value))}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="heading">Heading</label>
                <input
                    id="heading"
                    type="number"
                    className="form-control"
                    value={heading}
                    onChange={(e) => setHeading(parseFloat(e.target.value))}
                />
            </div>

            <div className="form-group mb-3">
                <label htmlFor="action">Action</label>
                <select
                    id="action"
                    className="form-select"
                    value={action}
                    onChange={(e) => setAction(e.target.value)}
                >
                    <option value="noAction">No Action</option>
                    <option value="takePhoto">Take Picture</option>
                    <option value="startRecord">Start Recording</option>
                    <option value="stopRecord">Stop Recording</option>
                </select>
            </div>

            <div className="d-flex justify-content-between">
                {/* Save Button */}
                <button
                    onClick={() => onSave({ id: waypoint.id, altitude, speed, angle, heading, action })}
                    className="btn btn-primary"
                >
                    Save
                </button>

                {/* Remove Button */}
                <button
                    onClick={() => onRemove(waypoint.id)}
                    className="btn btn-danger"
                >
                    Remove
                </button>

                {/* Submit Button */}
                <button
                    onClick={handleSubmit}
                    className="btn btn-success"
                >
                    Submit
                </button>
            </div>
        </div>
    );
};

export default WaypointInfoBox;
