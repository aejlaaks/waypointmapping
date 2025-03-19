// MapComponent.jsx
import React, { useCallback, useRef, useState, useEffect } from 'react';
import { GoogleMap, useJsApiLoader, DrawingManager, Polyline } from '@react-google-maps/api';
import { validateAndCorrectCoordinates } from '../services/JSFunctions';
import FlightParametersPanel from './FlightParametersPanel';
import MapToolbar from './MapToolbar';
import { useFlightParameters } from '../hooks/useFlightParameters';
import { useDrawingManager } from '../hooks/useDrawingManager';
import { useWaypointAPI } from '../hooks/useWaypointAPI';
import { MapProvider, useMapContext } from '../context/MapContext';

// Map configuration as static constants to prevent unnecessary reloads
// IMPORTANT: This must be defined outside the component to avoid recreation
const LIBRARIES = ['drawing', 'places'];
const MAP_CONTAINER_STYLE = { width: '100%', height: '100%' };
const DEFAULT_CENTER = { lat: 60.1699, lng: 24.9384 }; // Helsinki
const DEFAULT_ZOOM = 10;

// DrawingManager options - defined as static object to prevent recreations
const DRAWING_MANAGER_OPTIONS = {
  drawingControl: false,
  drawingMode: null,
  rectangleOptions: {
    fillColor: '#2196F3',
    fillOpacity: 0.5,
    strokeWeight: 2,
    clickable: true,
    editable: true,
    zIndex: 1,
  },
  circleOptions: {
    fillColor: '#FF9800',
    fillOpacity: 0.5,
    strokeWeight: 2,
    clickable: true,
    editable: true,
    zIndex: 1,
  },
  polylineOptions: {
    strokeColor: '#FF0000',
    strokeWeight: 2,
    clickable: true,
    editable: true,
    zIndex: 1,
  },
};

/**
 * Inner component that contains the map functionality
 */
const MapComponentInner = () => {
  // Custom hooks
  const flightParams = useFlightParameters();
  const { 
    mapRef, 
    drawingManagerRef, 
    genInfoWindowRef,
    path, 
    setPath,
    bounds,
    boundsType,
    selectedShape,
    clearAll
  } = useMapContext();
  
  // Initialize the custom hooks
  const { onDrawingManagerLoad, onDrawingComplete, enableDrawingMode, stopDrawing, parseShapeBounds } = useDrawingManager();
  const { generateWaypointsFromAPI, generateKml } = useWaypointAPI();
  
  // State
    const [startingIndex, setStartingIndex] = useState(1);
  const inputRef = useRef(null);
  const downloadLinkRef = useRef(null);
  const [googleLoaded, setGoogleLoaded] = useState(false);

  // Verify the Google Drawing library is available
    useEffect(() => {
    // Check if Google Maps library is loaded and the drawing namespace exists
    if (window.google && window.google.maps && window.google.maps.drawing) {
      console.log('Google Maps Drawing library is loaded');
      setGoogleLoaded(true);
      
      // Debug available overlay types
      console.log('OverlayType:', {
        RECTANGLE: google.maps.drawing.OverlayType.RECTANGLE,
        CIRCLE: google.maps.drawing.OverlayType.CIRCLE,
        POLYLINE: google.maps.drawing.OverlayType.POLYLINE
      });
            } else {
      console.error('Google Maps Drawing library is not loaded properly');
      setGoogleLoaded(false);
    }
  }, []);

  // Debug console for ref values
    useEffect(() => {
    console.log('Drawing Manager Ref:', drawingManagerRef.current);
    console.log('Map Ref:', mapRef.current);
  }, [drawingManagerRef.current, mapRef.current]);

  // Handler for map click
  const handleMapClick = useCallback((event) => {
    // Add new point to path for free drawing
        const newPoint = {
            lat: event.latLng.lat(),
            lng: event.latLng.lng(),
        };
        setPath((prevPath) => [...prevPath, newPoint]);
  }, [setPath]);

  // Handle map load
    const onLoad = useCallback((map) => {
    console.log('Map loaded:', map);
        mapRef.current = map;
    
    // Initialize the search box
        const searchBox = new window.google.maps.places.SearchBox(inputRef.current);

    // Initialize map properties
        mapRef.current.flags = []; // Initialize flags as an empty array
        mapRef.current.lines = []; // Initialize lines as an empty array
    genInfoWindowRef.current = new google.maps.InfoWindow({
            content: "message",
        });

    // Bias the SearchBox results towards current map's viewport
        map.addListener('bounds_changed', () => {
            searchBox.setBounds(map.getBounds());
        });

    // Listen for search box changes
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
  }, [mapRef, genInfoWindowRef]);

  // Handle drawing manager load
  const handleDrawingManagerLoad = useCallback((drawingManager) => {
    console.log('Drawing Manager loaded:', drawingManager);
    drawingManagerRef.current = drawingManager;
    
    // Verify the drawing manager has access to drawing modes
    console.log('Available drawing modes:', {
      RECTANGLE: google.maps.drawing.OverlayType.RECTANGLE,
      CIRCLE: google.maps.drawing.OverlayType.CIRCLE,
      POLYLINE: google.maps.drawing.OverlayType.POLYLINE,
      POLYGON: google.maps.drawing.OverlayType.POLYGON
    });
  }, [drawingManagerRef]);

  // Handle drawing complete event
  const handleOverlayComplete = useCallback((e) => {
    console.log('Drawing completed:', e);
    
    // Map Google Maps overlay types to our internal string representation
    let overlayType;
    switch (e.type) {
      case google.maps.drawing.OverlayType.RECTANGLE:
        overlayType = 'rectangle';
        break;
      case google.maps.drawing.OverlayType.CIRCLE:
        overlayType = 'circle';
        break;
      case google.maps.drawing.OverlayType.POLYLINE:
        overlayType = 'polyline';
        break;
      case google.maps.drawing.OverlayType.POLYGON:
        overlayType = 'polygon';
        break;
      default:
        overlayType = e.type; // Fallback to whatever was provided
    }
    
    console.log('Mapped overlay type:', overlayType);
    onDrawingComplete(e.overlay, overlayType);
    
    // Stop drawing mode after completion
    if (drawingManagerRef.current) {
      drawingManagerRef.current.setDrawingMode(null);
    }
  }, [onDrawingComplete, drawingManagerRef]);

  // Button click handlers with debugging
  const handleDrawRectangle = useCallback(() => {
    console.log('Draw Rectangle clicked');
    // Use our custom hook instead of direct access
    enableDrawingMode('rectangle');
  }, [enableDrawingMode]);

  const handleDrawCircle = useCallback(() => {
    console.log('Draw Circle clicked');
    // Use our custom hook instead of direct access
    enableDrawingMode('circle');
  }, [enableDrawingMode]);

  const handleDrawPolyline = useCallback(() => {
    console.log('Draw Polyline clicked');
    // Use our custom hook instead of direct access
    enableDrawingMode('polyline');
  }, [enableDrawingMode]);

  const handleStopDrawing = useCallback(() => {
    console.log('Stop Drawing clicked, drawingManagerRef:', drawingManagerRef.current);
    stopDrawing();
  }, [stopDrawing, drawingManagerRef]);

  // Submit form to generate waypoints
  const handleGenerateWaypoints = async () => {
    console.log('Generate Waypoints clicked');
    console.log('Current bounds:', bounds);
    console.log('Current bounds type:', boundsType);
    
    // Verify we have bounds before trying to generate waypoints
    if (!bounds) {
      alert('No bounds defined. Please draw a shape first.');
      return;
    }
    
    if (!boundsType) {
      alert('No bounds type defined. Please draw a shape first.');
      return;
    }
    
    // Validate coordinates based on bounds type
    let validCoordinates;
    try {
      if (boundsType === "rectangle" || boundsType === "polyline") {
        console.log('Parsing as rectangle or polyline:', bounds);
        validCoordinates = validateAndCorrectCoordinates(bounds);
      } else if (boundsType === "circle") {
        console.log('Parsing circle bounds:', bounds);
        
        // IMPORTANT: For circles, first try to use the cached center if available
        if (window.lastCircleCenter) {
          console.log('Found cached circle center coordinates:', window.lastCircleCenter);
          // Create a single coordinate with radius property from the cached values
          validCoordinates = [{
            Lat: window.lastCircleCenter.lat,
            Lng: window.lastCircleCenter.lng,
            lat: window.lastCircleCenter.lat,
            lng: window.lastCircleCenter.lng,
            radius: window.lastCircleCenter.radius,
            Radius: window.lastCircleCenter.radius
          }];
          
          console.log('Created circle data from cached coordinates:', validCoordinates);
        } else {
          // Try to parse from the bounds string as fallback
          const circleData = parseShapeBounds(bounds, boundsType);
          
          // Verify the data
          if (!circleData || !circleData.lat || !circleData.lng) {
            console.error('Failed to get valid circle data from bounds');
            alert('Error: Failed to determine circle center coordinates. Please try drawing the circle again.');
            return;
          }
          
          // Create a single coordinate with radius property
          validCoordinates = [{
            Lat: circleData.lat,
            Lng: circleData.lng,
            lat: circleData.lat,
            lng: circleData.lng,
            radius: circleData.radius,
            Radius: circleData.radius
          }];
          
          console.log('Processed circle data from bounds:', validCoordinates);
        }
      } else {
        console.log('Parsing with parseShapeBounds:', bounds, boundsType);
        validCoordinates = parseShapeBounds(bounds, boundsType);
      }
      
      console.log('Validated coordinates:', validCoordinates);
      
      // Check if validCoordinates is an array with length
      if (!validCoordinates || !Array.isArray(validCoordinates) || validCoordinates.length === 0) {
        throw new Error('Failed to parse coordinates from bounds');
      }
      
      // Ensure coordinates are in the correct format
      // This creates a new array with the necessary properties
      validCoordinates = validCoordinates.map(coord => {
        // Base coordinate object with Lat/Lng
        const newCoord = {
          Lat: Number(coord.Lat || coord.lat || 0),
          Lng: Number(coord.Lng || coord.lng || 0)
        };
        
        // For circle shape, ensure we include BOTH radius properties to handle different conventions
        if (boundsType === "circle") {
          newCoord.radius = Number(coord.radius || coord.Radius || 0);
          newCoord.Radius = Number(coord.radius || coord.Radius || 0);
        }
        
        return newCoord;
      });
      
    } catch (error) {
      console.error('Error parsing coordinates:', error);
      alert(`Error parsing coordinates: ${error.message}`);
      return;
    }

    // Get flight parameters
    const flightParameters = flightParams.getFlightParameters();
    console.log('Flight parameters from hook:', flightParameters);
    console.log('useEndpointsOnly specifically:', flightParameters.useEndpointsOnly);
    
    // Create request data with only primitive and safe-to-serialize values
    const requestData = {
      Bounds: validCoordinates,
      BoundsType: String(boundsType || ''),
      StartingIndex: Number(startingIndex || 1),
      Altitude: Number(flightParameters.altitude || 60),
      Speed: Number(flightParameters.speed || 2.5),
      Angle: Number(flightParameters.angle || -45),
      PhotoInterval: Number(flightParameters.interval || 2),
      Overlap: Number(flightParameters.overlap || 80),
      LineSpacing: Number(flightParameters.inDistance || 10),
      // Use explicit boolean check for both camelCase and PascalCase
      IsNorthSouth: flightParameters.isNorthSouth === true,
      isNorthSouth: flightParameters.isNorthSouth === true, // Add lowercase version too
      // Include both camelCase and PascalCase versions to ensure proper handling
      UseEndpointsOnly: flightParameters.useEndpointsOnly === true,
      useEndpointsOnly: flightParameters.useEndpointsOnly === true, // Add lowercase version too
      AllPointsAction: String(flightParameters.allPointsAction || 'noAction'),
      FinalAction: String(flightParameters.finalAction || '0'),
      FlipPath: Boolean(flightParameters.flipPath),
      UnitType: Number(flightParameters.unitType || 0)
    };
    
    // Verbose debug logging of the request
    console.log('useEndpointsOnly value from flightParameters:', flightParameters.useEndpointsOnly);
    console.log('useEndpointsOnly type:', typeof flightParameters.useEndpointsOnly);
    console.log('UseEndpointsOnly in requestData:', requestData.UseEndpointsOnly);
    console.log('Full API request payload:', JSON.stringify(requestData, null, 2));
    
    // Store the shape reference for UI updates only
    const shapeForUI = selectedShape;
    
    // Test JSON serialization before sending
    try {
      const serialized = JSON.stringify(requestData);
      console.log('Request data serialized successfully, length:', serialized.length);
    } catch (jsonError) {
      console.error('Cannot serialize request data:', jsonError);
      alert('Error: Cannot serialize waypoint data. Please try again with a simpler shape.');
      return;
    }
    
    console.log('Request data for API call:', requestData);

    try {
      // Call the API to generate waypoints
      const response = await generateWaypointsFromAPI(requestData);
      
      console.log('Raw API response:', response);
      
      // Process waypoints and add them to the map
      if (response && response.waypoints) {
        // Update starting index after generating waypoints
        setStartingIndex(prev => prev + 1);
        
        // Apply UI updates with the shape reference
        if (shapeForUI) {
          console.log('Keeping shape on map for reference');
        }
      }
    } catch (error) {
      console.error('Generate waypoints error:', error);
      alert('Error generating waypoints: ' + (error.message || error));
    }
  };

  const handleGenerateKml = useCallback(() => {
    console.log('Generate KML clicked');
    generateKml(downloadLinkRef);
  }, [generateKml, downloadLinkRef]);

  // Handle flight parameter changes
  const handleParameterChange = (paramName, value) => {
    // Get the setter function name based on parameter name
    const setterName = `set${paramName.charAt(0).toUpperCase() + paramName.slice(1)}`;
    
    // Call the appropriate setter from flightParams
    if (typeof flightParams[setterName] === 'function') {
      flightParams[setterName](value);
        }
    };

    return (
        <div className="flex-container">
      {/* Left panel for flight parameters */}
      <div className="side-panel">
        <h2 className="section-header">Drone Flight Planner</h2>
              
                    <input
          ref={inputRef}
                        type="text"
          placeholder="Search for a location"
          className="search-box"
        />
        <a ref={downloadLinkRef} style={{ display: 'none' }}>Download KML</a>
        
        <FlightParametersPanel 
          flightParameters={flightParams} 
          onValueChange={handleParameterChange}
        />
            </div>
              
      {/* Map container */}
      <div className="map-container">
                    <GoogleMap
          mapContainerStyle={MAP_CONTAINER_STYLE}
          center={DEFAULT_CENTER}
          zoom={DEFAULT_ZOOM}
          onLoad={onLoad}
          onClick={handleMapClick}
        >
          {/* Draw path for polyline */}
          {path.length > 0 && (
            <Polyline 
              path={path} 
              options={{ strokeColor: '#FF0000', strokeWeight: 2 }} 
            />
          )}
          
          {/* Drawing manager - Only render when Google is properly loaded */}
          {googleLoaded && window.google && window.google.maps && window.google.maps.drawing && (
                        <DrawingManager
              onLoad={handleDrawingManagerLoad}
              onOverlayComplete={handleOverlayComplete}
              options={DRAWING_MANAGER_OPTIONS}
            />
          )}
          
          {/* Map toolbar */}
          <MapToolbar
            onStopDrawing={handleStopDrawing}
            onGenerateWaypoints={handleGenerateWaypoints}
            onGenerateKml={handleGenerateKml}
            onDrawRectangle={handleDrawRectangle}
            onDrawCircle={handleDrawCircle}
            onDrawPolyline={handleDrawPolyline}
            onClearShapes={clearAll}
            startingIndex={startingIndex}
          />
                    </GoogleMap>
            </div>
        </div>
    );
};

/**
 * Main MapComponent that wraps the inner component with context providers
 */
const MapComponent = () => {
  const { isLoaded, loadError } = useJsApiLoader({
    googleMapsApiKey: import.meta.env.VITE_GOOGLE_MAPS_API_KEY, // Use environment variable
    libraries: LIBRARIES, // Use the static LIBRARIES array
  });

  if (loadError) {
    return <div className="loading-container">Error loading maps: {loadError.message}</div>;
  }

  if (!isLoaded) {
    return <div className="loading-container">Loading map...</div>;
  }

  return (
    <MapProvider>
      <MapComponentInner />
    </MapProvider>
  );
};

export default MapComponent;