import { useCallback } from 'react';
import { useMapContext } from '../context/MapContext';
import { parseBoundsString } from '../services/MapService';

/**
 * Custom hook to manage drawing operations
 */
export const useDrawingManager = (onShapeComplete) => {
  const { 
    setShapes, 
    setSelectedShape, 
    setBounds, 
    setBoundsType,
    drawingManagerRef, 
    mapRef,
    genInfoWindowRef
  } = useMapContext();

  // Handle drawing completion
  const onDrawingComplete = useCallback((shape, type) => {
    setShapes((prevShapes) => [...prevShapes, shape]);
    setSelectedShape(shape);

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
      const radius = shape.getRadius();
      
      // Log the actual center coordinates to verify they're real-world coordinates
      console.log('CIRCLE DEBUG: Actual circle center from Google Maps:', {
        lat: center.lat(),
        lng: center.lng(),
        radius: radius
      });
      
      // Store the center coordinates directly in a global variable for debugging and recovery
      window.lastCircleCenter = {
        lat: center.lat(),
        lng: center.lng(),
        radius: radius
      };
      
      // Verify the coordinates are not at (0,0)
      if (Math.abs(center.lat()) < 0.0001 && Math.abs(center.lng()) < 0.0001) {
        console.error('CRITICAL ERROR: Circle drawn at coordinates (0,0)! This is likely an error.');
        alert('Error: Circle appears to be at coordinates (0,0). Please try drawing the circle again at your intended location.');
      }
      
      // Format the coordinates with more precision to avoid rounding errors
      coordinates = `${center.lat().toFixed(8)},${center.lng().toFixed(8)}; radius: ${radius.toFixed(2)}`;
      
      console.log('CIRCLE DEBUG: Formatted circle coordinates string:', coordinates);
    } else if (type === 'polyline') {
      const path = shape.getPath();
      const pathCoordinates = [];
      for (let i = 0; i < path.getLength(); i++) {
        const point = path.getAt(i);
        pathCoordinates.push(`${point.lat()},${point.lng()}`);
      }
      coordinates = pathCoordinates.join(';');
    }
    
    setBounds(coordinates);
    setBoundsType(type);

    // Set the position of the info window to the center of the shape
    const position = type === 'circle' 
      ? shape.getCenter() 
      : (shape.getBounds ? shape.getBounds().getCenter() : null);

    // Set the content of the InfoWindow
    if (genInfoWindowRef.current && position) {
      genInfoWindowRef.current.setContent(`Coordinates: ${coordinates}`);
      genInfoWindowRef.current.setPosition(position);
      genInfoWindowRef.current.open(mapRef.current);
    }

    // Call the provided callback if it exists
    if (onShapeComplete) {
      onShapeComplete(shape, type, coordinates);
    }
  }, [setShapes, setSelectedShape, setBounds, setBoundsType, mapRef, genInfoWindowRef, onShapeComplete]);

  // Enable drawing mode
  const enableDrawingMode = useCallback((mode) => {
    if (!drawingManagerRef.current) {
      console.error('Drawing manager reference is not available');
      return;
    }

    console.log('Setting drawing mode to:', mode);
    
    // Check if google.maps.drawing is available
    if (!window.google || !window.google.maps || !window.google.maps.drawing) {
      console.error('Google Maps Drawing library is not loaded properly');
      return;
    }
    
    try {
      // Get the appropriate overlay type
      let overlayType = null;
      
      switch (mode) {
        case 'rectangle':
          overlayType = window.google.maps.drawing.OverlayType.RECTANGLE;
          break;
        case 'circle':
          overlayType = window.google.maps.drawing.OverlayType.CIRCLE;
          break;
        case 'polyline':
          overlayType = window.google.maps.drawing.OverlayType.POLYLINE;
          break;
        case 'polygon':
          overlayType = window.google.maps.drawing.OverlayType.POLYGON;
          break;
        case null:
          overlayType = null;
          break;
        default:
          console.warn(`Unknown drawing mode: ${mode}, setting to null`);
          overlayType = null;
      }
      
      console.log(`Setting drawing mode to: ${mode} (${overlayType})`);
      drawingManagerRef.current.setDrawingMode(overlayType);
      console.log('Drawing mode set successfully');
    } catch (error) {
      console.error('Error setting drawing mode:', error);
    }
  }, [drawingManagerRef]);

  // Stop drawing
  const stopDrawing = useCallback(() => {
    if (!drawingManagerRef.current) {
      console.error('Drawing manager reference is not available');
      return;
    }
    
    console.log('Stopping drawing mode');
    try {
      drawingManagerRef.current.setDrawingMode(null);
      console.log('Drawing mode stopped successfully');
    } catch (error) {
      console.error('Error stopping drawing mode:', error);
    }
  }, [drawingManagerRef]);

  // Initialize drawing manager when map loads
  const onDrawingManagerLoad = useCallback((drawingManager) => {
    drawingManagerRef.current = drawingManager;
  }, [drawingManagerRef]);

  // Parse bounds for API requests
  const parseShapeBounds = useCallback((bounds, boundsType) => {
    if (!bounds) {
      console.error('No bounds provided to parseShapeBounds');
      return [];
    }
    
    console.log(`Parsing bounds for ${boundsType}:`, bounds);
    
    try {
      if (boundsType === 'rectangle' || boundsType === 'polyline') {
        // Handle rectangle or polyline bounds
        return bounds.split(';').map(coord => {
          const trimmedCoord = coord.trim();
          const [lat, lng] = trimmedCoord.split(',').map(val => {
            const cleaned = val.trim();
            const parsed = parseFloat(cleaned);
            if (isNaN(parsed)) {
              throw new Error(`Invalid coordinate value: ${cleaned}`);
            }
            return parsed;
          });
          
          // Return with uppercase first letter for API compatibility
          return { Lat: lat, Lng: lng };
        });
      } else if (boundsType === 'circle') {
        // Handle circle bounds
        const circleData = parseBoundsString(bounds);
        if (!circleData) {
          throw new Error('Failed to parse circle bounds');
        }
        
        // For circles, create a set of points around the circumference
        const points = [];
        const numPoints = 24; // Use 24 points to approximate a circle
        
        for (let i = 0; i < numPoints; i++) {
          const angle = (i / numPoints) * 2 * Math.PI;
          
          // Calculate point on circle - radius is in meters, convert to rough lat/lng offset
          // This is a rough approximation that works for small circles
          const latOffset = (circleData.radius / 111320) * Math.cos(angle);
          const lngOffset = (circleData.radius / (111320 * Math.cos(circleData.lat * (Math.PI / 180)))) * Math.sin(angle);
          
          points.push({
            Lat: circleData.lat + latOffset,
            Lng: circleData.lng + lngOffset
          });
        }
        
        return points;
      }
      
      console.error('Unsupported bounds type:', boundsType);
      return [];
    } catch (error) {
      console.error('Error parsing shape bounds:', error);
      throw error; // Re-throw for handling in caller
    }
  }, []);

  return {
    onDrawingComplete,
    enableDrawingMode,
    stopDrawing,
    onDrawingManagerLoad,
    parseShapeBounds
  };
}; 