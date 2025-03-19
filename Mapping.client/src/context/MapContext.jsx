import { createContext, useContext, useState, useRef } from 'react';

// Create the context
const MapContext = createContext();

// Context provider component
export const MapProvider = ({ children }) => {
  // Map state
  const [shapes, setShapes] = useState([]);
  const [waypoints, setWaypoints] = useState([]);
  const [selectedShape, setSelectedShape] = useState(null);
  const [selectedMarker, setSelectedMarker] = useState(null);
  const [path, setPath] = useState([]);
  const [bounds, setBounds] = useState('');
  const [boundsType, setBoundsType] = useState(['rectangle']);
  
  // Refs
  const mapRef = useRef(null);
  const drawingManagerRef = useRef(null);
  const genInfoWindowRef = useRef(null);

  // Clear all shapes and waypoints
  const clearAll = () => {
    shapes.forEach((shape) => shape.setMap(null));
    setShapes([]);
    setBounds('');
    setBoundsType('');
    
    // Remove all markers from the map
    if (mapRef.current && mapRef.current.flags) {
      mapRef.current.flags.forEach((marker) => {
        marker.setMap(null); // Remove marker from the map
      });
      mapRef.current.flags = []; // Clear the markers array
    }

    // Remove all routes (polylines) from the map
    if (mapRef.current && mapRef.current.lines) {
      mapRef.current.lines.forEach((line) => {
        line.setMap(null); // Remove polyline (route) from the map
      });
      mapRef.current.lines = []; // Clear the routes array
    }

    // Clear waypoints state
    setWaypoints([]);
  };

  // Redraw flight paths
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

  // Update marker icon
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

  // Redraw markers
  const redrawMarkers = () => {
    if (mapRef.current && mapRef.current.flags) {
      mapRef.current.flags.forEach(waypoint => {
        waypoint.marker.setLabel(`${waypoint.id}`);
      });
    }
  };

  // Handle waypoint click
  const handleWaypointClick = (marker) => {
    setSelectedMarker(marker);
  };

  // Handle waypoint drag end
  const handleWaypointDragEnd = (marker) => {
    marker.lat = marker.getPosition().lat();
    marker.lng = marker.getPosition().lng();
    setWaypoints(prevWaypoints =>
      prevWaypoints.map(way => (way.id === marker.id ? { ...marker, lat: marker.lat, lng: marker.lng } : way))
    );
    redrawFlightPaths();
  };

  // Export the context value
  const value = {
    // State
    shapes,
    setShapes,
    waypoints,
    setWaypoints,
    selectedShape,
    setSelectedShape,
    selectedMarker,
    setSelectedMarker,
    path,
    setPath,
    bounds,
    setBounds,
    boundsType,
    setBoundsType,
    
    // Refs
    mapRef,
    drawingManagerRef,
    genInfoWindowRef,
    
    // Functions
    clearAll,
    redrawFlightPaths,
    updateMarkerIcon,
    redrawMarkers,
    handleWaypointClick,
    handleWaypointDragEnd
  };

  return <MapContext.Provider value={value}>{children}</MapContext.Provider>;
};

// Custom hook to use the map context
export const useMapContext = () => {
  const context = useContext(MapContext);
  if (context === undefined) {
    throw new Error('useMapContext must be used within a MapProvider');
  }
  return context;
};

export default MapContext; 