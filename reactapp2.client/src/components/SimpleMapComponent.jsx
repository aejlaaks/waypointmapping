import React, { useState, useRef, useCallback, useEffect } from 'react';
import { GoogleMap, useJsApiLoader, DrawingManager, Marker, Polyline, InfoWindow } from '@react-google-maps/api';
import MapControls from './MapControls';
import WaypointInfoBox from './WaypointInfoBox';
import WaypointList from './WaypointList';
import { createWaypointModel, generateWaypoints, updateWaypoint, deleteWaypoint } from '../services/WaypointService';
import { createPolyline, createMarker } from '../services/MapService';
import { createKmlDownload } from '../services/KmlService';
import '../App.css';

// Map configuration
const mapConfig = {
  center: {
    lat: 60.1699, // Helsinki
    lng: 24.9384
  },
  zoom: 14,
  mapTypeId: 'satellite'
};

// Google Maps API configuration
const googleMapsConfig = {
  googleMapsApiKey: 'AIzaSyCbrrhYSaiyels_EyP05HalRiWcev73g0E',
  libraries: ['drawing', 'places']
};

// Drawing manager options
const drawingManagerOptions = {
  drawingControl: true,
  drawingControlOptions: {
    drawingModes: ['rectangle', 'polygon', 'circle']
  },
  rectangleOptions: {
    fillColor: 'rgba(255, 0, 0, 0.2)',
    strokeWeight: 2,
    editable: true,
    draggable: true
  },
  polygonOptions: {
    fillColor: 'rgba(255, 0, 0, 0.2)',
    strokeWeight: 2,
    editable: true,
    draggable: true
  },
  circleOptions: {
    fillColor: 'rgba(255, 0, 0, 0.2)',
    strokeWeight: 2,
    editable: true,
    draggable: true
  }
};

const SimpleMapComponent = () => {
  // Load Google Maps API
  const { isLoaded } = useJsApiLoader(googleMapsConfig);
  
  // State
  const [waypoints, setWaypoints] = useState([]);
  const [selectedWaypoint, setSelectedWaypoint] = useState(null);
  const [selectedShape, setSelectedShape] = useState(null);
  const [flightPath, setFlightPath] = useState([]);
  const [infoWindowPosition, setInfoWindowPosition] = useState(null);
  const [toast, setToast] = useState({show: false, message: '', type: ''});
  const [isDarkMode, setIsDarkMode] = useState(true);
  
  // Refs
  const mapRef = useRef(null);
  const drawingManagerRef = useRef(null);
  const polylineRef = useRef(null);
  
  // Map load callback
  const handleMapLoad = useCallback((map) => {
    mapRef.current = map;
  }, []);
  
  // Drawing manager load callback
  const handleDrawingManagerLoad = useCallback((drawingManager) => {
    drawingManagerRef.current = drawingManager;
    
    // Add event listeners for shape creation
    if (drawingManager) {
      google.maps.event.addListener(drawingManager, 'overlaycomplete', (event) => {
        // Stop drawing
        drawingManager.setDrawingMode(null);
        
        // Set up the shape
        const shape = event.overlay;
        shape.type = event.type;
        
        // Select the shape
        setSelectedShape(shape);
      });
    }
  }, []);
  
  // Handle waypoint click
  const handleWaypointClick = (waypoint) => {
    setSelectedWaypoint(waypoint);
    setInfoWindowPosition({ lat: waypoint.lat, lng: waypoint.lng });
  };
  
  // Handle waypoint drag end
  const handleWaypointDragEnd = (waypoint, event) => {
    const newLat = event.latLng.lat();
    const newLng = event.latLng.lng();
    
    const updatedWaypoints = waypoints.map(wp => 
      wp.id === waypoint.id 
        ? { ...wp, lat: newLat, lng: newLng } 
        : wp
    );
    
    setWaypoints(updatedWaypoints);
    updateFlightPath(updatedWaypoints);
  };
  
  // Close info window
  const handleInfoWindowClose = () => {
    setSelectedWaypoint(null);
    setInfoWindowPosition(null);
  };
  
  // Update waypoint
  const handleUpdateWaypoint = (id, updatedFields) => {
    const updatedWaypoints = waypoints.map(wp => 
      wp.id === id 
        ? { ...wp, ...updatedFields } 
        : wp
    );
    
    setWaypoints(updatedWaypoints);
    
    if (selectedWaypoint && selectedWaypoint.id === id) {
      setSelectedWaypoint({ ...selectedWaypoint, ...updatedFields });
    }
    
    // Update in database if needed
    try {
      updateWaypoint(id, updatedFields);
    } catch (error) {
      console.error('Error updating waypoint:', error);
    }
  };
  
  // Delete waypoint
  const handleDeleteWaypoint = (id) => {
    const updatedWaypoints = waypoints.filter(wp => wp.id !== id);
    setWaypoints(updatedWaypoints);
    
    if (selectedWaypoint && selectedWaypoint.id === id) {
      setSelectedWaypoint(null);
      setInfoWindowPosition(null);
    }
    
    updateFlightPath(updatedWaypoints);
    
    // Delete from database if needed
    try {
      deleteWaypoint(id);
    } catch (error) {
      console.error('Error deleting waypoint:', error);
    }
  };
  
  // Update flight path
  const updateFlightPath = (waypointsArray) => {
    const path = waypointsArray.map(wp => ({ lat: wp.lat, lng: wp.lng }));
    setFlightPath(path);
    
    // Update polyline on map
    if (polylineRef.current) {
      polylineRef.current.setMap(null);
    }
    
    if (path.length > 0 && mapRef.current) {
      polylineRef.current = createPolyline(mapRef.current, path, "#FF0000", 2);
    }
  };
  
  // Generate waypoints from shape
  const handleGenerateWaypoints = (params) => {
    if (!selectedShape) {
      alert('Please draw a shape first');
      return;
    }
    
    let shapeBounds;
    
    if (selectedShape.type === 'rectangle') {
      const bounds = selectedShape.getBounds();
      shapeBounds = {
        north: bounds.getNorthEast().lat(),
        east: bounds.getNorthEast().lng(),
        south: bounds.getSouthWest().lat(),
        west: bounds.getSouthWest().lng()
      };
    } else if (selectedShape.type === 'polygon') {
      // Get polygon path
      const path = selectedShape.getPath();
      const points = [];
      
      for (let i = 0; i < path.getLength(); i++) {
        const point = path.getAt(i);
        points.push({ lat: point.lat(), lng: point.lng() });
      }
      
      shapeBounds = { points };
    } else if (selectedShape.type === 'circle') {
      const center = selectedShape.getCenter();
      shapeBounds = {
        center: { lat: center.lat(), lng: center.lng() },
        radius: selectedShape.getRadius()
      };
    }
    
    // Generate waypoints with shapeBounds and params
    generateWaypoints({
      shape: selectedShape.type,
      bounds: shapeBounds,
      ...params
    })
      .then(generatedWaypoints => {
        // Convert to our waypoint model
        const newWaypoints = generatedWaypoints.map((wp, index) => 
          createWaypointModel(
            index + 1,
            wp.lat,
            wp.lng,
            {
              altitude: params.altitude,
              speed: params.speed,
              angle: params.angle,
              action: params.allPointsAction
            }
          )
        );
        
        setWaypoints(newWaypoints);
        updateFlightPath(newWaypoints);
        
        // Add toast notification
        setToast({
          show: true,
          message: `Generated ${newWaypoints.length} waypoints successfully!`,
          type: 'success'
        });
        
        // Automatically hide toast after 3 seconds
        setTimeout(() => {
          setToast({show: false, message: '', type: ''});
        }, 3000);
        
        // Remove the selected shape
        if (selectedShape) {
          selectedShape.setMap(null);
          setSelectedShape(null);
        }
      })
      .catch(error => {
        console.error('Error generating waypoints:', error);
        setToast({
          show: true,
          message: 'Failed to generate waypoints. Please try again.',
          type: 'error'
        });
        
        setTimeout(() => {
          setToast({show: false, message: '', type: ''});
        }, 3000);
      });
  };
  
  // Download KML
  const handleDownloadKml = () => {
    if (waypoints.length === 0) {
      alert('No waypoints to download');
      return;
    }
    
    const kmlUrl = createKmlDownload(waypoints);
    
    if (kmlUrl) {
      // Create temporary link and click it
      const link = document.createElement('a');
      link.href = kmlUrl;
      link.download = 'drone_waypoints.kml';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      
      // Revoke URL to free memory
      setTimeout(() => URL.revokeObjectURL(kmlUrl), 100);
    } else {
      alert('Failed to create KML file');
    }
  };
  
  // After the component mounts
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', isDarkMode ? 'dark' : 'light');
  }, [isDarkMode]);
  
  // Modify the renderWaypoints function or the waypoint rendering part
  const renderWaypoints = () => {
    return waypoints.map((waypoint, index) => {
      // Determine if this is a vertex waypoint (first, last, or any point where direction changes significantly)
      const isVertex = index === 0 || index === waypoints.length - 1 || 
        (index > 0 && index < waypoints.length - 1 && 
          Math.abs(calculateHeadingChange(waypoints[index-1], waypoint, waypoints[index+1])) > 10);
      
      return (
        <Marker
          key={waypoint.id}
          position={{ lat: waypoint.lat, lng: waypoint.lng }}
          onClick={() => handleWaypointClick(waypoint)}
          draggable={true}
          onDragEnd={(e) => handleWaypointDragEnd(waypoint, e)}
          icon={{
            path: google.maps.SymbolPath.FORWARD_CLOSED_ARROW,
            rotation: waypoint.heading || 0,
            scale: isVertex ? 6 : 4,
            fillColor: isVertex ? '#FF4500' : '#3CB371',
            fillOpacity: 0.8,
            strokeWeight: 1,
            strokeColor: '#ffffff'
          }}
          label={{
            text: waypoint.id.toString(),
            color: '#ffffff',
            fontSize: '10px',
            fontWeight: 'bold'
          }}
        />
      );
    });
  };
  
  // Add function to calculate heading change (to detect vertices)
  const calculateHeadingChange = (prev, current, next) => {
    const heading1 = Math.atan2(
      current.lng - prev.lng,
      current.lat - prev.lat
    ) * 180 / Math.PI;
    
    const heading2 = Math.atan2(
      next.lng - current.lng,
      next.lat - current.lat
    ) * 180 / Math.PI;
    
    return (heading2 - heading1 + 360) % 360;
  };
  
  if (!isLoaded) {
    return <div>Loading Maps...</div>;
  }
  
  return (
    <div className="map-container">
      <div className="row">
        <div className="col-md-4">
          <MapControls 
            onGenerateWaypoints={handleGenerateWaypoints}
            onDownloadKml={handleDownloadKml}
            hasWaypoints={waypoints.length > 0}
          />
          
          <WaypointList 
            waypoints={waypoints}
            onUpdate={handleUpdateWaypoint}
            onDelete={handleDeleteWaypoint}
          />
        </div>
        
        <div className="col-md-8">
          <GoogleMap
            mapContainerStyle={{ width: '100%', height: '600px' }}
            center={mapConfig.center}
            zoom={mapConfig.zoom}
            mapTypeId={mapConfig.mapTypeId}
            onLoad={handleMapLoad}
          >
            {/* Drawing Manager */}
            <DrawingManager
              options={drawingManagerOptions}
              onLoad={handleDrawingManagerLoad}
            />
            
            {/* Flight Path Polyline */}
            {flightPath.length > 1 && (
              <Polyline
                path={flightPath}
                options={{
                  strokeColor: '#FF0000',
                  strokeOpacity: 1.0,
                  strokeWeight: 2
                }}
              />
            )}
            
            {/* Waypoint Markers */}
            {renderWaypoints()}
            
            {/* Info Window for Selected Waypoint */}
            {selectedWaypoint && infoWindowPosition && (
              <InfoWindow
                position={infoWindowPosition}
                onCloseClick={handleInfoWindowClose}
              >
                <div>
                  <WaypointInfoBox
                    waypoint={selectedWaypoint}
                    onSave={(updatedWaypoint) => {
                      handleUpdateWaypoint(updatedWaypoint.id, updatedWaypoint);
                      handleInfoWindowClose();
                    }}
                    onRemove={(id) => {
                      handleDeleteWaypoint(id);
                      handleInfoWindowClose();
                    }}
                  />
                </div>
              </InfoWindow>
            )}
          </GoogleMap>
        </div>
      </div>
      <div className="theme-toggle">
        <button 
          className="btn btn-sm btn-outline-secondary" 
          onClick={() => setIsDarkMode(!isDarkMode)}
        >
          {isDarkMode ? 'Switch to Light Mode' : 'Switch to Dark Mode'}
        </button>
      </div>
      {toast.show && (
        <div className={`toast-notification ${toast.type}`}>
          {toast.message}
        </div>
      )}
    </div>
  );
};

export default SimpleMapComponent; 