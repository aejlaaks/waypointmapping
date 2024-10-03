// MapComponent.jsx
import React, { useCallback, useRef, useState, useEffect } from 'react';
import { GoogleMap, useJsApiLoader, DrawingManager, InfoWindow, Marker, Polyline } from '@react-google-maps/api';
import WaypointInfoBox from './WaypointInfoBox';
import axios from 'axios'; // Import axios for making API calls
import "../App.css";
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;;


const libraries = ['drawing', 'places'];

const containerStyle = {
    width: '100%',
    height: '600px',
};

const center = {
    lat: 60.1699, // Example center location (Helsinki)
    lng: 24.9384,
};
const inputStyle = {
    marginBottom: '10px',
};

const stopDrawingButtonStyle = {
    padding: '10px 20px',
    backgroundColor: '#dc3545',
    color: '#fff',
    border: 'none',
    borderRadius: '5px',
    cursor: 'pointer',
    marginBottom: '10px',
};


const topLeftButtonStyle = {
    position: 'absolute',
    top: '10px',
    left: '10px',
    zIndex: 1000,
    padding: '10px 20px',
    backgroundColor: '#007bff',
    color: '#fff',
    border: 'none',
    borderRadius: '5px',
    cursor: 'pointer',
};

const topRightButtonStyle = {
    position: 'absolute',
    top: '10px',
    right: '10px',
    zIndex: 1000,
    padding: '10px 20px',
    backgroundColor: '#28a745',
    color: '#fff',
    border: 'none',
    borderRadius: '5px',
    cursor: 'pointer',
};

const bottomLeftButtonStyle = {
    position: 'absolute',
    bottom: '10px',
    left: '10px',
    zIndex: 1000,
    padding: '10px 20px',
    backgroundColor: '#dc3545',
    color: '#fff',
    border: 'none',
    borderRadius: '5px',
    cursor: 'pointer',
};

const bottomRightButtonStyle = {
    position: 'absolute',
    bottom: '10px',
    right: '10px',
    zIndex: 1000,
    padding: '10px 20px',
    backgroundColor: '#ffc107',
    color: '#fff',
    border: 'none',
    borderRadius: '5px',
    cursor: 'pointer',
};
const drawingButtonContainerStyle = {
    position: 'absolute',
    top: '10px',
    left: '10px',
    zIndex: 1000, // Ensure the buttons are in front of the map
    display: 'flex',
    flexDirection: 'column',
};

const buttonStyle = {
    padding: '10px 20px',
    backgroundColor: '#007bff',
    color: '#fff',
    border: 'none',
    borderRadius: '5px',
    cursor: 'pointer',
    marginBottom: '10px',
};


const flexContainerStyle = {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    width: '100%',
};

const inputContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    width: '50%',
    padding: '10px',
};

function MapComponent() {
    const { isLoaded } = useJsApiLoader({
        googleMapsApiKey: 'AIzaSyCbrrhYSaiyels_EyP05HalRiWcev73g0E', // Replace with your API key
        libraries,
    });

    const [shapes, setShapes] = useState([]);
    const [searchLocation, setSearchLocation] = useState('');
    const [unitType, setUnitType] = useState('0');
    const [altitude, setAltitude] = useState(60);
    const [speed, setSpeed] = useState(2.5);
    const [angle, setAngle] = useState(-45);
    const [focalLength, setFocalLength] = useState(24); // Camera focal length in mm
    const [sensorWidth, setSensorWidth] = useState(36); // Camera sensor width in mm
    const [sensorHeight, setSensorHeight] = useState(24); // Camera sensor height in mm
    const [photoInterval, setPhotoInterval] = useState(2); // Photo interval in seconds
    const [overlap, setOverlap] = useState(80);
    const [bounds, setBounds] = useState('');
    const [boundsType, setBoundsType] = useState(["rectangle"]);
    const [startingIndex, setStartingIndex] = useState(1);
    const [allPointsAction, setAllPointsAction] = useState('noAction');
    const [finalAction, setFinalAction] = useState('0');
    const [flipPath, setFlipPath] = useState(false);
    const [interval, setInterval] = useState(3);
    const [inDistance, setInDistance] = useState(10); // New state variable for in_distance
    const [selectedShape, setSelectedShape] = useState(null);
    const [selectedMarker, setSelectedMarker] = useState(null);
    const [infoWindowPosition, setInfoWindowPosition] = useState(null);
    const [infoWindowVisible, setInfoWindowVisible] = useState(false);
    const [selectedWaypoint, setSelectedWaypoint] = useState(null);
    const [distanceBetweenPaths, setDistanceBetweenPaths] = useState(0);
    // Integroi waypointit ja selectedShape tiloina
    const [waypoints, setWaypoints] = useState([]); // vastaa map.flags
    const drawingManagerRef = useRef(null);
    const mapRef = useRef(null);
    const genInfoWindow = useRef(null); // Ref for genInfoWindow
    const downloadLinkRef = useRef(null); // Ref for the download link
    const [in_allPointsAction, setInAllPointsAction] = useState('takePhoto'); // New state variable for in_allPointsAction

    const buttonStyle = {
        position: 'absolute',
        top: '10px',
        left: '10px',
        zIndex: 1000, // Ensure the button is in front of the map
        padding: '10px 20px',
        backgroundColor: '#007bff',
        color: '#fff',
        border: 'none',
        borderRadius: '5px',
        cursor: 'pointer',
    };
    

    // Alkuperäinen mapin asennus ja event handlerit pidetään
    useEffect(() => {
        // Kartan asennus logiikka
        // Kun waypointit päivittyvät, päivitetään markerit
        if (mapRef.current) {
            mapRef.current.addListener("click", handleMapClick);
        }

        waypoints.forEach(marker => {
            google.maps.event.addListener(marker, "click", () => handleWaypointClick(marker));
            google.maps.event.addListener(marker, "mouseup", () => handleWaypointDragEnd(marker));
            google.maps.event.addListener(marker, 'dragend', () => handleWaypointDragEnd(marker));
        });

        //Initialize or clear flags
        if (mapRef.current) {
            if (!mapRef.current.flags) {
                mapRef.current.flags = [];
            } else {
                mapRef.current.flags = [];
            }
        }
        
        return () => {
            if (mapRef.current) {
                google.maps.event.clearListeners(mapRef.current, "click");
            }

            waypoints.forEach(marker => {
                google.maps.event.clearListeners(marker, "click");
                google.maps.event.clearListeners(marker, "mouseup");
                google.maps.event.clearListeners(marker, 'dragend');
            });
        };
        RedrawMarkers();
    }, [waypoints]);

    // Function to calculate Distance Between Paths
    const calculateDistanceBetweenPaths = (altitude, overlap, focalLength, sensorWidth) => {
        const fovWidth = 2 * (altitude * Math.tan(Math.atan(sensorWidth / (2 * focalLength))));
        return fovWidth * (1 - overlap / 100);
    };

    // Function to calculate Speed
    const calculateSpeed = (altitude, overlap, focalLength, sensorHeight, photoInterval) => {
        const fovHeight = 2 * (altitude * Math.tan(Math.atan(sensorHeight / (2 * focalLength))));
        return (fovHeight * (1 - overlap / 100)) / photoInterval;
    };

    const stopDrawing = () => {
        if (drawingManagerRef.current) {
            drawingManagerRef.current.setDrawingMode(null); // Stop drawing mode
        }
    };

    // Recalculate when overlap changes
    useEffect(() => {
        const newDistanceBetweenPaths = calculateDistanceBetweenPaths(altitude, overlap, focalLength, sensorWidth);
        const newSpeed = calculateSpeed(altitude, overlap, focalLength, sensorHeight, photoInterval);

        setDistanceBetweenPaths(newDistanceBetweenPaths);
        setSpeed(newSpeed);
    }, [altitude, overlap, focalLength, sensorWidth, sensorHeight, photoInterval]);
  
    // Function to update the info box listeners
    const infoBoxUpdateListeners = () => {

        const saveButton = document.getElementById("editWaypointSave");
        const removeButton = document.getElementById("editWaypointRemove");

        if (saveButton) {
            saveButton.addEventListener("click", () => {
                const id = document.getElementById("editWaypointID").value;
                WaypointEditorSave(id);
            });
        }

        if (removeButton) {
            removeButton.addEventListener("click", () => {
                const id = document.getElementById("editWaypointID").value;
                WaypointEditiorRemove(id);
            });
        }

        return () => {
            if (saveButton) {
                saveButton.removeEventListener("click", () => {
                    const id = document.getElementById("editWaypointID").value;
                    WaypointEditorSave(id);
                });
            }

            if (removeButton) {
                removeButton.removeEventListener("click", () => {
                    const id = document.getElementById("editWaypointID").value;
                    WaypointEditiorRemove(id);
                });
            }
        }
        document.getElementById("editWaypointSave").addEventListener("click", () => {
            const id = document.getElementById("editWaypointID").value;
            WaypointEditorSave(id);
        });

        document.getElementById("editWaypointRemove").addEventListener("click", () => {
            const id = document.getElementById("editWaypointID").value;
            WaypointEditiorRemove(id);
        });
    };



    // Handle waypoint click event
    const handleWaypointClick = (marker) => {
        setSelectedMarker(marker); // Päivitä valittu reittipiste
    };

    // Function to handle waypoint drag end
    const handleWaypointDragEnd = (marker) => {
        marker.lat = marker.getPosition().lat();
        marker.lng = marker.getPosition().lng();
        setWaypoints(prevWaypoints =>
            prevWaypoints.map(way => (way.id === marker.id ? { ...marker, lat: marker.lat, lng: marker.lng } : way))
        );
        redrawFlightPaths();
    };

    // Function to handle map click
    const handleMapClick = () => {
        setSelectedMarker(null); // Tyhjennä valittu reittipiste
    };

    // Funktio markerin päivittämiseen kartalla
    const updateMarkerIcon = (waypoint) => {
        const updateMarker = {
            path: 'M 230 80 A 45 45, 0, 1, 0, 275 125 L 275 80 Z',
            fillOpacity: 0.8,
            fillColor: 'blue',
            anchor: new google.maps.Point(228, 125),
            strokeWeight: 3,
            strokeColor: 'white',
            scale: 0.5,
            rotation: waypoint.heading - 45,
            labelOrigin: new google.maps.Point(228, 125),
        };
        waypoint.marker.setIcon(updateMarker);
    };

    // Tallenna ja poista funktiot
    const handleWaypointSave = (updatedWaypoint) => {
        setWaypoints(prevWaypoints =>
            prevWaypoints.map(waypoint =>
                waypoint.id === updatedWaypoint.id ? updatedWaypoint : waypoint
            )
        );
    };

    const handleWaypointRemove = (id) => {
        setWaypoints(prevWaypoints => prevWaypoints.filter(waypoint => waypoint.id !== id));
    };

    // Muodon poistaminen, vastaava ShapeEditiorRemove funktiota
    const ShapeEditiorRemove = () => {
        if (selectedShape) {
            selectedShape.setMap(null); // Poistetaan valittu muoto kartalta
            setSelectedShape(null);
        }
    };

    // Markerien uudelleenpiirto kartalla
    const RedrawMarkers = () => {
        waypoints.forEach(waypoint => {
            waypoint.marker.setLabel(`${waypoint.id}`);
        });
    };

 
    const onLoad = useCallback((map) => {
        mapRef.current = map;
        const input = document.getElementById('pac-input');
        const searchBox = new window.google.maps.places.SearchBox(input);
        mapRef.current.flags = []; // Initialize flags as an empty array
        mapRef.current.lines = []; // Initialize lines as an empty array
        genInfoWindow.current = new google.maps.InfoWindow({
            content: "message",
        });

        // Bias the SearchBox results towards current map's viewport.
        map.addListener('bounds_changed', () => {
            searchBox.setBounds(map.getBounds());
        });

        searchBox.addListener('places_changed', () => {
            const places = searchBox.getPlaces();
            if (places.length === 0) return;

            // Focus the map on the first result
            const place = places[0];
            if (place.geometry) {
                map.setCenter(place.geometry.location);
                map.setZoom(15);
            }
        });
    }, []);;

    const onDrawingComplete = (shape, type) => {
        setShapes((prevShapes) => [...prevShapes, shape]);
        setSelectedShape(shape); // Set the selected shape

        let coordinates = '';
        if (type === 'polygon' || type === 'rectangle') {
            const bounds = shape.getBounds();
            const northEast = bounds.getNorthEast();
            const southWest = bounds.getSouthWest();
            const northWest = new google.maps.LatLng(northEast.lat(), southWest.lng());
            const southEast = new google.maps.LatLng(southWest.lat(), northEast.lng());

            coordinates = `${northEast.lat()},${northEast.lng()};${southEast.lat()},${southEast.lng()};${southWest.lat()},${southWest.lng()};${northWest.lat()},${northWest.lng()}`;
        } else if (type === 'circle') {
            const center = shape.getCenter();
            coordinates = `${center.lat()},${center.lng()}; radius: ${shape.getRadius()}`;
        }
        setBounds(coordinates);
        setBoundsType(type);

        setStartingIndex((prevIndex) => prevIndex + 1);

        // Set the position of the info window to the center of the shape
        const position = type === 'circle' ? shape.getCenter() : shape.getBounds().getCenter();
        setInfoWindowPosition(position);
        //setInfoWindowVisible(true);

        // Set the content of the InfoWindow
        if (genInfoWindow.current) {
            genInfoWindow.current.setContent(`Coordinates: ${coordinates}`);
            genInfoWindow.current.setPosition(position);
            genInfoWindow.current.open(mapRef.current);
        }
    };

    const handleDrawingModeChange = (mode) => {
        if (drawingManagerRef.current) {
            drawingManagerRef.current.setDrawingMode(mode);
        }
    };

    const UpdateTimeEstimate = () => {
        // Assuming you have access to the necessary variables to calculate the time estimate
        const totalDistance = mapRef.current.flags.reduce((acc, marker, index, array) => {
            if (index === 0) return acc;
            const prevMarker = array[index - 1];
            const distance = google.maps.geometry.spherical.computeDistanceBetween(
                new google.maps.LatLng(prevMarker.lat, prevMarker.lng),
                new google.maps.LatLng(marker.lat, marker.lng)
            );
            return acc + distance;
        }, 0);

        const estimatedTime = totalDistance / speed; // Assuming speed is in meters per second
        console.log(`Estimated Time: ${estimatedTime} seconds`);
    };

    const redrawFlightPaths = () => {
        // Clear existing flight paths
        if (mapRef.current && mapRef.current.lines) {
            mapRef.current.lines.forEach(line => line.setMap(null));
            mapRef.current.lines = [];
        }

        // Redraw flight paths based on current markers
        const flightPoints = mapRef.current.flags.map(marker => ({
            lat: marker.lat,
            lng: marker.lng
        }));

        const flightPath = new google.maps.Polyline({
            path: flightPoints,
            geodesic: true,
            strokeColor: "#FF0000",
            strokeOpacity: 1.0,
            strokeWeight: 2,
        });

        flightPath.setMap(mapRef.current);
        mapRef.current.lines.push(flightPath);
    };
    const handleClearShapes = () => {
        shapes.forEach((shape) => shape.setMap(null));
        setShapes([]);
        setBounds('');
        setBoundsType('');
        setStartingIndex(1);
    };

    const validateAndCorrectCoordinates = (coordinatesString) => {
        try {
            // Split the string by semicolon to get individual coordinate pairs
            const coordinatePairs = coordinatesString.split(';');

            // Map each pair to an object with lat and lng properties
            const coordinatesArray = coordinatePairs.map(pair => {
                const [lat, lng] = pair.split(',').map(Number);
                if (isNaN(lat) || isNaN(lng)) {
                    throw new Error("Invalid coordinate values");
                }
                return { lat, lng };
            });

            return coordinatesArray;
        } catch (error) {
            console.error("Error parsing coordinates string:", error);
            return null;
        }
    };

    const measure = (lat1, lon1, lat2, lon2) => {
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
  
    const WaypointEditorSave = (id) => {
        const map = mapRef.current; // Get the map object from the ref

        map.flags.forEach((flag, i) => {
            if (flag.id == document.getElementById("selectedWaypointId").innerHTML) {
                if (flag.heading != document.getElementById("editWaypointHeading").value) {
                    const updateMarker = {
                        path: 'M 230 80 A 45 45, 0, 1, 0, 275 125 L 275 80 Z',
                        fillOpacity: 0.8,
                        fillColor: 'blue',
                        anchor: new google.maps.Point(228, 125),
                        strokeWeight: 3,
                        strokeColor: 'white',
                        scale: 0.5,
                        rotation: document.getElementById("editWaypointHeading").value - 45,
                        labelOrigin: new google.maps.Point(228, 125),
                    };

                    flag.setIcon(updateMarker);
                }

                // Update waypoint properties
                flag.altitude = document.getElementById("editWaypointAltitude").value;
                flag.speed = document.getElementById("editWaypointSpeed").value;
                flag.angle = document.getElementById("editWaypointAngle").value;
                flag.heading = document.getElementById("editWaypointHeading").value;
                flag.action = document.getElementById("editWaypointAction").value;

                if (document.getElementById("editWaypointID").value > map.flags.length) {
                    document.getElementById("editWaypointID").value = map.flags.length;
                }

                if (flag.id != document.getElementById("editWaypointID").value) {
                    let perc = 0;
                    map.flags.forEach((otherFlag, x) => {
                        const originalId = flag.id;
                        if (otherFlag.id == document.getElementById("editWaypointID").value || perc == 1) {
                            if (otherFlag.id < originalId) {
                                otherFlag.id = parseInt(otherFlag.id) + 1;
                                perc = 1;
                            } else if (otherFlag.id >= originalId) {
                                for (let y = otherFlag.id - 1; y >= originalId; y--) {
                                    map.flags[y].id = parseInt(map.flags[y].id) - 1;
                                    perc = 1;
                                }

                                flag.id = document.getElementById("editWaypointID").value;
                                RedrawMarkers();
                                map.flags.sort((a, b) => a.id - b.id);
                                redrawFlightPaths();
                                return;
                            }
                        }
                    });

                    flag.id = document.getElementById("editWaypointID").value;
                    RedrawMarkers();
                    map.flags.sort((a, b) => a.id - b.id);
                    redrawFlightPaths();
                }
                return;
            }
        });
    };

    const WaypointEditiorRemove = () => {
        const map = mapRef.current; // Get the map object from the ref

        map.flags.forEach((flag, i) => {
            if (flag.id == document.getElementById("selectedWaypointId").innerHTML) {
                map.flags.forEach((otherFlag, x) => {
                    if (flag.id < otherFlag.id) {
                        otherFlag.id -= 1;
                    }
                });

                RedrawMarkers(map);

                flag.setMap(null);
                map.flags.splice(i, 1);

                redrawFlightPaths();
                flagCount -= 1;
                return;
            }
        });
    };


    const GenerateWaypointInfoboxText = (waypointMarker) => {
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

        return `<div><h2 class="text-center" id="selectedWaypointId">
            ${waypointMarker.id}</h2><div class="text-center">
            ${waypointMarker.lat}, ${waypointMarker.lng}</div><br/>
            Altitude:<br/><input type="text" id="editWaypointAltitude" value="${waypointMarker.altitude}" /><span class="unitsLabel">Meters</span><br/>
            Speed:<br/><input type="text" id="editWaypointSpeed" value="${waypointMarker.speed}" /><span class="unitsLabel">Meters</span>/s<br/>
            Gimbal Angle:<br/><input type="text" id="editWaypointAngle" value="${waypointMarker.angle}" />Degrees<br/>
            Heading:<br/><input type="text" id="editWaypointHeading" value="${waypointMarker.heading}" />Degrees North<br/>
            Action:<br/><select id="editWaypointAction">${select}</select><br />
            Waypoint Number:<br/><input type="text" id="editWaypointID" value="${waypointMarker.id}" /><br/><br/>
            <div class="text-center">
            <button class="btn btn-success" id="editWaypointSave"">Save</button><span> </span>
            <button class="btn btn-danger" id"editWaypointRemovee">Remove</button></div></div>`;
    };

    const GenerateShapeInfoboxText = (shape) => {
        return `<div class="text-center"><h4>Generate Waypoints For Shape?</h4>
            <button class="btn btn-success" onclick="submitFormFetch()">Generate</button><span>   </span>
            <button class="btn btn-danger" onclick="ShapeEditiorRemove()">Remove</button></div>`;
    };


    const generateKml = async () => {
        const requestData = {
            FlyToWaylineMode: "safely",
            FinishAction: finalAction,
            ExitOnRCLost: "executeLostAction",
            ExecuteRCLostAction: "goBack",
            GlobalTransitionalSpeed: speed,
            DroneInfo: {
                DroneEnumValue: 1,
                DroneSubEnumValue: 1
            },
            Waypoints: mapRef.current.flags.map((wp, index) => ({
                Index: index,
                Latitude: wp.lat,
                Longitude: wp.lng,
                ExecuteHeight: wp.altitude,
                WaypointSpeed: wp.speed,
                WaypointHeadingMode: "smoothTransition",
                WaypointHeadingAngle: wp.Heading,
                WaypointHeadingPathMode: "followBadArc",
                WaypointTurnMode: "toPointAndStopWithContinuityCurvature",
                WaypointTurnDampingDist: "0",
                Action: wp.action
            })),
            ActionGroups: [] // Add your action groups here if any
        };

        try {
            const response = await axios.post(`${apiBaseUrl}/api/KMZ/generate`, requestData, {
                responseType: 'blob', // Important for file download
            });

            // Create a URL for the blob
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = downloadLinkRef.current;
            link.href = url;
            link.setAttribute('download', 'generated.kml');
            link.click();
            window.URL.revokeObjectURL(url); // Clean up the URL object

        } catch (error) {
            console.error('Error generating KML:', error);
        }
    };

    const submitFormFetch = () => {

        const validCoordinates = validateAndCorrectCoordinates(bounds);
        // Ensure the state is updated before making the API call
        const allPointsActionValue = document.getElementById('in_allPointsAction').value;
        setAllPointsAction(allPointsActionValue);
        //const boundsObject = JSON.parse(validCoordinates);

        // Extract necessary data from shapes to avoid circular references
        const shapesData = shapes.map(shape => {
            if (boundsType === 'polygon' || boundsType === 'rectangle') {
                const bounds = shape.bounds;
                return {
                    type: shape.type,
                    bounds: {
                        northEast: { lat: bounds.getNorthEast().lat(), lng: bounds.getNorthEast().lng() },
                        southWest: { lat: bounds.getSouthWest().lat(), lng: bounds.getSouthWest().lng() }
                    }
                };
            } else if (boundsType === 'circle') {
                const center = shape.getCenter();
                return {
                    type: shape.type,
                    center: { lat: center.lat(), lng: center.lng() },
                    radius: shape.getRadius()
                };
            }
            return null;
        }).filter(shape => shape !== null);

        const data = {
            shapes: shapesData,
            Bounds: validCoordinates,
            BoundsType: boundsType,
            startingIndex: startingIndex,
            unitType: unitType,
            altitude: altitude,
            speed: speed,
            overlap: overlap,
            allPointsAction: allPointsAction,
            finalAction: finalAction,
            flipPath: flipPath,
            interval: interval,
            in_distance: inDistance,
            angle:angle,


        };


        fetch(`${apiBaseUrl}/api/waypoints/generatePoints`, {
            method: "post",
            headers: {
                "Content-Type": "application/json"
            },
                    body: JSON.stringify(data)

        })
            .then((res) => res.text())
            .then((txt) => {
                window.scrollTo(0, 0);
                var generatedPoints = JSON.parse(txt);

                let flightPoints = [];
                let flagCount = startingIndex;

                for (var i = 0; i < generatedPoints.length; i++) {
                    const responseMarker = {
                        path: 'M 230 80 A 45 45, 0, 1, 0, 275 125 L 275 80 Z',
                        fillOpacity: 0.8,
                        fillColor: 'blue',
                        anchor: new google.maps.Point(228, 125),
                        strokeWeight: 3,
                        strokeColor: 'white',
                        scale: 0.5,
                        rotation: generatedPoints[i].heading - 45,
                        labelOrigin: new google.maps.Point(228, 125),
                    };

                    var genWaypointMarker = new google.maps.Marker({
                        position: {
                            lat: generatedPoints[i].latitude, lng: generatedPoints[i].longitude
                        },
                        map: mapRef.current,
                        label: {
                            text: generatedPoints[i].id.toString(),
                            color: "white"
                        },
                        draggable: true,
                        icon: responseMarker,
                        id: generatedPoints[i].id
                    });

                    genWaypointMarker.lng = generatedPoints[i].longitude;
                    genWaypointMarker.lat = generatedPoints[i].latitude;
                    genWaypointMarker.altitude = generatedPoints[i].altitude;
                    genWaypointMarker.speed = generatedPoints[i].speed;
                    genWaypointMarker.heading = generatedPoints[i].heading;
                    genWaypointMarker.angle = generatedPoints[i].gimbalAngle;
                    genWaypointMarker.action = generatedPoints[i].action;

                    var marker = genWaypointMarker;

                    google.maps.event.addListener(marker, "click", function (e) {
                        genInfoWindow.current.close();
                        genInfoWindow.current.setContent(GenerateWaypointInfoboxText(this));
                        genInfoWindow.current.open(this.map, this);
                        infoBoxUpdateListeners();

                        document.getElementById("selectedWaypointId").innerHTML = this.id;
                        document.getElementById("editWaypointAltitude").value = this.altitude;
                        document.getElementById("editWaypointSpeed").value = this.speed;
                        document.getElementById("editWaypointAngle").value = this.angle;
                        document.getElementById("editWaypointHeading").value = this.heading;
                        document.getElementById("editWaypointAction").value = this.action;
                        document.getElementById("editWaypointID").value = this.id;

                        setSelectedMarker(this); // Update selectedMarker state
                    });

                    google.maps.event.addListener(marker, "mouseup", function (e) {
                        setSelectedMarker(this); // Update selectedMarker state
                        redrawFlightPaths();
                    });

                    google.maps.event.addListener(marker, 'dragend', function (e) {
                        this.lat = this.getPosition().lat();
                        this.lng = this.getPosition().lng();
                        genInfoWindow.current.close();
                        genInfoWindow.current.setContent(GenerateWaypointInfoboxText(this));
                        genInfoWindow.current.open(this.map, this);
                    });

                    mapRef.current.addListener("click", () => {
                        genInfoWindow.current.close();
                        setSelectedMarker(null); // Clear selectedMarker state
                    });

                    mapRef.current.flags.push(genWaypointMarker);

                    flightPoints.push({ lat: generatedPoints[i].latitude, lng: generatedPoints[i].longitude });
                }

                const flightPath = new google.maps.Polyline({
                    path: flightPoints,
                    geodesic: true,
                    strokeColor: "#FF0000",
                    strokeOpacity: 1.0,
                    strokeWeight: 2,
                });

                selectedShape.setMap(null);

                flightPath.setMap(mapRef.current);
                document.getElementById("in_startingIndex").value = flagCount;

                mapRef.current.lines.push(flightPath);
                redrawFlightPaths();
                UpdateTimeEstimate();
            })
            .catch((err) => {
                alert(err);
            });
    };

    const enableDrawingMode = (mode) => {
        if (drawingManagerRef.current) {
            drawingManagerRef.current.setDrawingMode(mode);
        }
    };

    return (
        <div style={flexContainerStyle}>
            <div style={inputContainerStyle}>
              
                <a ref={downloadLinkRef} style={{ display: 'none' }}>Download KML</a>
                {/* Add your input fields here */}
                <label>
                    Search Location
                    <input
                        type="text"
                        placeholder="Search Location"
                        value={searchLocation}
                        onChange={(e) => setSearchLocation(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Altitude
                    <input
                        type="number"
                        placeholder="Altitude"
                        value={altitude}
                        onChange={(e) => setAltitude(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Speed
                    <input
                        type="number"
                        placeholder="Speed"
                        value={speed}
                        onChange={(e) => setSpeed(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Angle
                    <input
                        type="number"
                        placeholder="Angle"
                        value={angle}
                        onChange={(e) => setAngle(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Focal Length
                    <input
                        type="number"
                        placeholder="Focal Length"
                        value={focalLength}
                        onChange={(e) => setFocalLength(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Sensor Width
                    <input
                        type="number"
                        placeholder="Sensor Width"
                        value={sensorWidth}
                        onChange={(e) => setSensorWidth(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Sensor Height
                    <input
                        type="number"
                        placeholder="Sensor Height"
                        value={sensorHeight}
                        onChange={(e) => setSensorHeight(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Photo Interval
                    <input
                        type="number"
                        placeholder="Photo Interval"
                        value={photoInterval}
                        onChange={(e) => setPhotoInterval(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Overlap
                    <input
                        type="number"
                        placeholder="Overlap"
                        value={overlap}
                        onChange={(e) => setOverlap(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    Interval
                    <input
                        type="number"
                        placeholder="Interval"
                        value={interval}
                        onChange={(e) => setInterval(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    In Distance
                    <input
                        type="number"
                        placeholder="In Distance"
                        value={inDistance}
                        onChange={(e) => setInDistance(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                <label>
                    All Points Action
                    <input
                        type="text"
                        id="in_allPointsAction"
                        placeholder="All Points Action"
                        value={in_allPointsAction}
                        onChange={(e) => setInAllPointsAction(e.target.value)}
                        style={inputStyle}
                    />
                </label>
                {/* Add more inputs as needed */}
            </div>
            <div style={{ width: '70%', margin: '50px' }}>
              
                {isLoaded && (
                    <GoogleMap
                        mapContainerStyle={containerStyle}
                        center={center}
                        zoom={10}
                        onLoad={onLoad} // Add this line to set the onLoad callback
                    >
                        <DrawingManager
                            onLoad={(drawingManager) => (drawingManagerRef.current = drawingManager)}
                            onOverlayComplete={(e) => onDrawingComplete(e.overlay, e.type)}
                            options={{
                                drawingControl: false,
                                polygonOptions: {
                                    fillColor: '#2196F3',
                                    fillOpacity: 0.5,
                                    strokeWeight: 2,
                                    clickable: true,
                                    editable: true,
                                    zIndex: 1,
                                },
                            }}
                        />
                        {waypoints.map(waypoint => (
                            <Marker key={waypoint.id} position={{ lat: waypoint.lat, lng: waypoint.lng }} />
                        ))}
                        <button style={topLeftButtonStyle} onClick={stopDrawing}>Stop Drawing</button>
                        <input type="hidden" id="in_startingIndex" value={startingIndex} />
                        <button style={bottomLeftButtonStyle} onClick={submitFormFetch}>Generate waypoints</button>
                        <button style={bottomRightButtonStyle} onClick={generateKml}>Generate KML</button>
                        <button style={topRightButtonStyle} onClick={() => enableDrawingMode('rectangle')}>Draw Rectangle</button>
                    </GoogleMap>
                )}
            </div>
        </div>
    );
}

export default MapComponent;