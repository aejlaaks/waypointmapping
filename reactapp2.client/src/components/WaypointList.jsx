import React, { useState } from 'react';
import { updateWaypoint, deleteWaypoint } from '../services/api';

const WaypointList = ({ waypoints, setWaypoints, setFlightPath }) => {
    const handleUpdate = async (id, updatedWaypoint) => {
        try {
            await updateWaypoint(id, updatedWaypoint);
            const updatedWaypoints = waypoints.map(wp => wp.id === id ? { ...wp, ...updatedWaypoint } : wp);
            setWaypoints(updatedWaypoints);
            const path = updatedWaypoints.map(wp => ({ lat: wp.latitude, lng: wp.longitude }));
            setFlightPath(path);
        } catch (error) {
            console.error('Error updating waypoint:', error);
        }
    };

    const handleDelete = async (id) => {
        try {
            await deleteWaypoint(id);
            const filteredWaypoints = waypoints.filter(wp => wp.id !== id);
            setWaypoints(filteredWaypoints);
            const path = filteredWaypoints.map(wp => ({ lat: wp.latitude, lng: wp.longitude }));
            setFlightPath(path);
        } catch (error) {
            console.error('Error deleting waypoint:', error);
        }
    };

    return (
        <div className="mt-4">
            <h3>Waypoints</h3>
            <table className="table table-striped">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Latitude</th>
                        <th>Longitude</th>
                        <th>Altitude</th>
                        <th>Speed</th>
                        <th>Heading</th>
                        <th>Gimbal Angle</th>
                        <th>Action</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {waypoints.map(wp => (
                        <tr key={wp.id}>
                            <td>{wp.id}</td>
                            <td>{wp.latitude}</td>
                            <td>{wp.longitude}</td>
                            <td>
                                <input
                                    type="number"
                                    value={wp.altitude}
                                    onChange={(e) => handleUpdate(wp.id, { altitude: parseFloat(e.target.value) })}
                                    className="form-control"
                                />
                            </td>
                            <td>
                                <input
                                    type="number"
                                    value={wp.speed}
                                    onChange={(e) => handleUpdate(wp.id, { speed: parseFloat(e.target.value) })}
                                    className="form-control"
                                />
                            </td>
                            <td>
                                <input
                                    type="number"
                                    value={wp.heading}
                                    onChange={(e) => handleUpdate(wp.id, { heading: parseFloat(e.target.value) })}
                                    className="form-control"
                                />
                            </td>
                            <td>
                                <input
                                    type="number"
                                    value={wp.gimbalAngle}
                                    onChange={(e) => handleUpdate(wp.id, { gimbalAngle: parseFloat(e.target.value) })}
                                    className="form-control"
                                />
                            </td>
                            <td>
                                <select
                                    value={wp.action}
                                    onChange={(e) => handleUpdate(wp.id, { action: e.target.value })}
                                    className="form-select"
                                >
                                    <option value="noAction">No Action</option>
                                    <option value="takePhoto">Take Picture</option>
                                    <option value="startRecord">Start Recording</option>
                                    <option value="stopRecord">Stop Recording</option>
                                </select>
                            </td>
                            <td>
                                <button className="btn btn-danger btn-sm" onClick={() => handleDelete(wp.id)}>Delete</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
};

export default WaypointList;
