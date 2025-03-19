// JSFunctions.js

export const calculateDistanceBetweenPaths = (altitude, overlap, fov) => {
    const overlapFactor = (1 - overlap / 100);
    const fovRadians = (fov / 2) * (Math.PI / 180); // Muutetaan FOV radiaaneiksi
    const groundWidth = 2 * altitude * Math.tan(fovRadians);
    const newDistance = groundWidth * overlapFactor;
    return newDistance;
};

export const calculateSpeed = (altitude, overlap, focalLength, sensorHeight, photoInterval) => {
    const overlapFactor = (1 - overlap / 100);
    const vfovRadians = 2 * Math.atan(sensorHeight / (2 * focalLength));
    const groundHeight = 2 * altitude * Math.tan(vfovRadians / 2);
    const speed = (groundHeight * overlapFactor) / photoInterval;
    return speed;
};

export const validateAndCorrectCoordinates = (coordinatesString) => {
    try {
        if (!coordinatesString || typeof coordinatesString !== 'string') {
            console.error("Invalid coordinates string:", coordinatesString);
            return null;
        }
        
        // Split the string by semicolon to get individual coordinate pairs
        const coordinatePairs = coordinatesString.split(';');
        
        if (coordinatePairs.length < 2) {
            console.error("Not enough coordinate pairs:", coordinatesString);
            return null;
        }

        // Map each pair to an object with Lat and Lng properties for the API
        const coordinatesArray = coordinatePairs.map(pair => {
            // Clean up extra spaces
            const trimmedPair = pair.trim();
            
            // Split by comma
            const [lat, lng] = trimmedPair.split(',').map(val => {
                // Clean and parse as number
                const cleaned = val.trim();
                const parsed = parseFloat(cleaned);
                if (isNaN(parsed)) {
                    throw new Error(`Invalid coordinate value: ${cleaned}`);
                }
                return parsed;
            });
            
            // Validate latitude and longitude ranges
            if (lat < -90 || lat > 90) {
                throw new Error(`Latitude out of range: ${lat}`);
            }
            
            if (lng < -180 || lng > 180) {
                throw new Error(`Longitude out of range: ${lng}`);
            }
            
            // Return in the format expected by the API
            return { Lat: lat, Lng: lng };
        });
        
        console.log("Validated coordinates:", coordinatesArray);
        return coordinatesArray;
    } catch (error) {
        console.error("Error parsing coordinates string:", error);
        throw error; // Re-throw to handle in calling code
    }
};

export const measure = (lat1, lon1, lat2, lon2) => {
    const R = 6378.137; // Radius of earth in KM
    const dLat = lat2 * Math.PI / 180 - lat1 * Math.PI / 180;
    const dLon = lon2 * Math.PI / 180 - lon1 * Math.PI / 180;
    const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    const d = R * c;
    return d * 1000; // Distance in meters
};

export const GenerateWaypointInfoboxText = (waypointMarker) => {
    let select = '';
    if (waypointMarker.action == "noAction") {
        select = '<option selected value="noAction">No Action</option><option value="takePhoto">Take Picture</option><option value="startRecord">Start Recording</option><option value="stopRecord">Stop Recording</option>';
    } else if (waypointMarker.action == "takePhoto") {
        select = '<option value="noAction">No Action</option><option selected value="takePhoto">Take Picture</option><option value="startRecord">Start Recording</option><option value="stopRecord">Stop Recording</option>';
    } else if (waypointMarker.action == "startRecord") {
        select = '<option value="noAction">No Action</option><option value="takePhoto">Take Picture</option><option selected value="startRecord">Start Recording</option><option value="stopRecord">Stop Recording</option>';
    } else if (waypointMarker.action == "stopRecord") {
        select = '<option value="noAction">No Action</option><option value="takePhoto">Take Picture</option><option value="startRecord">Start Recording</option><option selected value="stopRecord">Stop Recording</option>';
    }
    return `<div style="color: black;"><h2 class="text-center" id="selectedWaypointId" style="color: black;">
    ${waypointMarker.id}</h2><div class="text-center" style="color: black;">
    ${waypointMarker.lat}, ${waypointMarker.lng}</div><br/>
    Altitude:<br/><input type="text" id="editWaypointAltitude" value="${waypointMarker.altitude}" /><span class="unitsLabel">Meters</span><br/>
    Speed:<br/><input type="text" id="editWaypointSpeed" value="${waypointMarker.speed}" /><span class="unitsLabel">Meters</span>/s<br/>
    Gimbal Angle:<br/><input type="text" id="editWaypointAngle" value="${waypointMarker.angle}" />Degrees<br/>
    Heading:<br/><input type="text" id="editWaypointHeading" value="${waypointMarker.heading}" />Degrees North<br/>
    Action:<br/><select id="editWaypointAction">${select}</select><br />
    Waypoint Number:<br/><input type="text" id="editWaypointID" value="${waypointMarker.id}" /><br/><br/>
    <div class="text-center" style="color: black;">
    <button class="btn btn-success" id="editWaypointSave"">Save</button><span> </span>
    <button class="btn btn-danger" id="editWaypointRemovee">Remove</button></div></div>`;

};

export const GenerateShapeInfoboxText = (shape) => {
    return `<div class="text-center"><h4>Generate Waypoints For Shape?</h4>
    <button class="btn btn-success" onclick="submitFormFetch()">Generate</button><span>   </span>
    <button class="btn btn-danger" onclick="ShapeEditiorRemove()">Remove</button></div>`;
};
