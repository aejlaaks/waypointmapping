import { useCallback } from 'react';
import { generateWaypoints } from '../services/WaypointService';
import axios from 'axios';
import { useMapContext } from '../context/MapContext';
import { GenerateWaypointInfoboxText } from '../services/JSFunctions';

// Get API base URL from environment variables
const apiBaseUrl = process.env.NODE_ENV === 'production'
  ? process.env.REACT_APP_API_BASE_URL 
  : import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

// Log the API base URL for debugging
console.log('API Base URL:', apiBaseUrl);

/**
 * Hook to handle all waypoint API operations
 */
export const useWaypointAPI = () => {
  const { 
    mapRef, 
    genInfoWindowRef, 
    setSelectedShape,
    redrawFlightPaths
  } = useMapContext();

  /**
   * Generate waypoints from server
   */
  const generateWaypointsFromAPI = useCallback(async (requestData) => {
    try {
      // Clone the request data to ensure we have a clean object without circular references
      const cleanRequest = {
        Bounds: Array.isArray(requestData.Bounds) 
          ? requestData.Bounds.map(coord => {
              // Create a base coordinate with Lat/Lng
              const cleanCoord = {
                Lat: Number(coord.Lat || coord.lat || 0),
                Lng: Number(coord.Lng || coord.lng || 0)
              };
              
              // For circle shapes, preserve the radius property
              if (requestData.BoundsType === 'circle' && (coord.radius || coord.Radius)) {
                cleanCoord.radius = Number(coord.radius || coord.Radius || 0);
                cleanCoord.Radius = Number(coord.radius || coord.Radius || 0);
              }
              
              return cleanCoord;
            })
          : [],
        BoundsType: String(requestData.BoundsType || ''),
        StartingIndex: Number(requestData.startingIndex || 1),
        Altitude: Number(requestData.altitude || 60),
        Speed: Number(requestData.speed || 2.5),
        Angle: Number(requestData.angle || -45),
        PhotoInterval: Number(requestData.photoInterval || 2),
        Overlap: Number(requestData.overlap || 80),
        LineSpacing: Number(requestData.inDistance || 10),
        // Use the standardized property name IsNorthSouth that matches the server-side model
        IsNorthSouth: Boolean(requestData.isNorthSouth || requestData.NorthSouthDirection),
        UseEndpointsOnly: Boolean(requestData.useEndpointsOnly),
        AllPointsAction: String(requestData.allPointsAction || 'noAction'),
        Action: String(requestData.allPointsAction || 'noAction'),
        FinalAction: String(requestData.finalAction || '0'),
        FlipPath: Boolean(requestData.flipPath || false),
        UnitType: Number(requestData.unitType || 0)
      };
      
      // For circle bounds, add the radius property to the first coordinate
      if (cleanRequest.BoundsType === 'circle' && cleanRequest.Bounds.length > 0) {
        // Verify that the original bounds has radius information
        const originalBounds = requestData.Bounds;
        if (originalBounds && originalBounds.length > 0 && originalBounds[0].radius) {
          // Make sure we assign the exact radius from the original request
          cleanRequest.Bounds[0].Radius = originalBounds[0].radius;
          
          // URGENT CIRCLE DEBUG
          console.log('URGENT CIRCLE DEBUG - Center before API call:', {
            Lat: cleanRequest.Bounds[0].Lat,
            Lng: cleanRequest.Bounds[0].Lng,
            Radius: cleanRequest.Bounds[0].Radius
          });
          
          // Add both lowercase and uppercase properties to ensure compatibility
          cleanRequest.Bounds[0].lat = cleanRequest.Bounds[0].Lat;
          cleanRequest.Bounds[0].lng = cleanRequest.Bounds[0].Lng;
          cleanRequest.Bounds[0].radius = cleanRequest.Bounds[0].Radius;
          
          console.log('Added radius information to circle bounds:', cleanRequest.Bounds[0].Radius);
          console.log('Circle center coordinates:', {
            Lat: cleanRequest.Bounds[0].Lat,
            Lng: cleanRequest.Bounds[0].Lng
          });
        }
      }
      
      console.log('Prepared clean request:', cleanRequest);
      
      // Validate request data before sending
      if (!cleanRequest.Bounds || cleanRequest.Bounds.length === 0) {
        throw new Error('No bounds provided for waypoint generation');
      }
      
      if (!cleanRequest.BoundsType) {
        throw new Error('Bounds type is required');
      }
      
      // CIRCLE SPECIFIC: Check for (0,0) coordinates which are definitely wrong
      if (cleanRequest.BoundsType === 'circle' && 
          cleanRequest.Bounds.length > 0 && 
          cleanRequest.Bounds[0].Lat === 0 && 
          cleanRequest.Bounds[0].Lng === 0) {
        
        console.error('CRITICAL ERROR: Circle center is at (0,0) in final request. This is definitely wrong!');
        
        // Try to use the global cache if available
        if (window.lastCircleCenter) {
          console.log('Replacing (0,0) with cached circle center:', window.lastCircleCenter);
          cleanRequest.Bounds[0].Lat = window.lastCircleCenter.lat;
          cleanRequest.Bounds[0].Lng = window.lastCircleCenter.lng;
          cleanRequest.Bounds[0].radius = window.lastCircleCenter.radius;
          cleanRequest.Bounds[0].Radius = window.lastCircleCenter.radius;
        } else {
          // Use a default location if all else fails
          console.warn('No cached coordinates available. Using Helsinki as fallback location');
          cleanRequest.Bounds[0].Lat = 60.1699;
          cleanRequest.Bounds[0].Lng = 24.9384;
        }
      }
      
      // Generate waypoints with clean data
      const generatedPoints = await generateWaypoints(cleanRequest);
      console.log('Received waypoints from API:', generatedPoints);
      
      if (!generatedPoints || generatedPoints.length === 0) {
        throw new Error('No waypoints returned from the server');
      }
      
      // Debug logging for circle shape
      if (cleanRequest.BoundsType === 'circle') {
        console.log('CIRCLE DEBUG: Got waypoints for circle:', generatedPoints.length);
        // Log the first few waypoints to check their format
        for (let i = 0; i < Math.min(3, generatedPoints.length); i++) {
          console.log(`CIRCLE DEBUG: Waypoint ${i}:`, JSON.stringify(generatedPoints[i]));
        }
      }
      
      // Clear previous waypoints before adding new ones
      if (mapRef.current && mapRef.current.flags) {
        // Remove existing markers
        for (let i = 0; i < mapRef.current.flags.length; i++) {
          mapRef.current.flags[i].setMap(null);
        }
        mapRef.current.flags = [];
      }
      
      // Clear previous flight paths
      if (mapRef.current && mapRef.current.lines) {
        // Remove existing polylines
        for (let i = 0; i < mapRef.current.lines.length; i++) {
          mapRef.current.lines[i].setMap(null);
        }
        mapRef.current.lines = [];
      }
      
      let flightPoints = [];
      const flagCount = cleanRequest.StartingIndex || 1;

      // Process each waypoint returned from the API
      let validWaypoints = 0;
      for (let i = 0; i < generatedPoints.length; i++) {
        const point = generatedPoints[i];
        
        // Validate waypoint data
        if (!point) {
          console.error('Invalid waypoint data (null or undefined):', point);
          continue; // Skip this waypoint
        }
        
        // Handle both property naming conventions (Lat/Lng vs latitude/longitude)
        const latitude = point.Lat !== undefined ? point.Lat : 
                        (point.lat !== undefined ? point.lat : 
                        (point.Latitude !== undefined ? point.Latitude : 
                        (point.latitude !== undefined ? point.latitude : null)));
        
        const longitude = point.Lng !== undefined ? point.Lng : 
                         (point.lng !== undefined ? point.lng : 
                         (point.Longitude !== undefined ? point.Longitude : 
                         (point.longitude !== undefined ? point.longitude : null)));
        
        if (latitude === null || longitude === null) {
          console.error('Invalid waypoint coordinates:', point);
          continue; // Skip this waypoint
        }
        
        // Circle-specific debugging
        if (cleanRequest.BoundsType === 'circle' && i % 10 === 0) {
          console.log(`CIRCLE DEBUG: Processing waypoint ${i} at ${latitude},${longitude}`);
        }
        
        // Check for valid latitude/longitude ranges
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) {
          console.error(`Invalid coordinate range: lat=${latitude}, lng=${longitude}`);
          continue; // Skip this waypoint
        }
        
        validWaypoints++;
        
        // Get ID using various property names
        const id = point.Index !== undefined ? point.Index : 
                  (point.index !== undefined ? point.index : 
                  (point.Id !== undefined ? point.Id : 
                  (point.id !== undefined ? point.id : i + 1)));
        
        // Get altitude using various property names
        const altitude = point.Alt !== undefined ? point.Alt : 
                        (point.alt !== undefined ? point.alt : 
                        (point.Altitude !== undefined ? point.Altitude : 
                        (point.altitude !== undefined ? point.altitude : 60)));
        
        // Create marker icon
        const responseMarker = {
          path: 'M 230 80 A 45 45, 0, 1, 0, 275 125 L 275 80 Z',
          fillOpacity: 0.8,
          fillColor: 'blue',
          anchor: new google.maps.Point(228, 125),
          strokeWeight: 3,
          strokeColor: 'white',
          scale: 0.3,
          rotation: (point.Heading || point.heading || 0) - 45,
          labelOrigin: new google.maps.Point(228, 125),
        };

        // Create the marker
        const genWaypointMarker = new google.maps.Marker({
          position: {
            lat: latitude, 
            lng: longitude
          },
          map: mapRef.current,
          label: {
            text: id.toString(),
            color: "white"
          },
          draggable: true,
          icon: responseMarker,
          id: id
        });

        // Store additional data on the marker
        genWaypointMarker.lng = longitude;
        genWaypointMarker.lat = latitude;
        genWaypointMarker.altitude = altitude;
        genWaypointMarker.speed = point.Speed || point.speed || 2.5;
        genWaypointMarker.heading = point.Heading || point.heading || 0;
        genWaypointMarker.angle = point.GimbalAngle || point.gimbalAngle || -45;
        genWaypointMarker.action = point.Action || point.action || 'noAction';

        // Add event listeners to the marker
        google.maps.event.addListener(genWaypointMarker, "click", function (e) {
          genInfoWindowRef.current.close();
          genInfoWindowRef.current.setContent(GenerateWaypointInfoboxText(this));
          genInfoWindowRef.current.open(this.map, this);
          
          // Safely try to update form fields if they exist
          try {
            const selectedId = document.getElementById("selectedWaypointId");
            if (selectedId) selectedId.innerHTML = this.id;
            
            const editAlt = document.getElementById("editWaypointAltitude");
            if (editAlt) editAlt.value = this.altitude;
            
            const editSpeed = document.getElementById("editWaypointSpeed");
            if (editSpeed) editSpeed.value = this.speed;
            
            const editAngle = document.getElementById("editWaypointAngle");
            if (editAngle) editAngle.value = this.angle;
            
            const editHeading = document.getElementById("editWaypointHeading");
            if (editHeading) editHeading.value = this.heading;
            
            const editAction = document.getElementById("editWaypointAction");
            if (editAction) editAction.value = this.action;
            
            const editId = document.getElementById("editWaypointID");
            if (editId) editId.value = this.id;
          } catch (formError) {
            console.error('Error updating waypoint form:', formError);
          }
        });

        google.maps.event.addListener(genWaypointMarker, "mouseup", function (e) {
          redrawFlightPaths();
        });

        google.maps.event.addListener(genWaypointMarker, 'dragend', function (e) {
          this.lat = this.getPosition().lat();
          this.lng = this.getPosition().lng();
          genInfoWindowRef.current.close();
          genInfoWindowRef.current.setContent(GenerateWaypointInfoboxText(this));
          genInfoWindowRef.current.open(this.map, this);
        });

        // Add the marker to the map's flags array
        mapRef.current.flags.push(genWaypointMarker);
        flightPoints.push({ lat: latitude, lng: longitude });
      }

      // Create a polyline connecting all waypoints
      const flightPath = new google.maps.Polyline({
        path: flightPoints,
        geodesic: true,
        strokeColor: "#FF0000",
        strokeOpacity: 1.0,
        strokeWeight: 2,
      });
      
      // Circle-specific debug
      if (cleanRequest.BoundsType === 'circle') {
        console.log(`CIRCLE DEBUG: Created ${validWaypoints} valid waypoints out of ${generatedPoints.length} total`);
        console.log(`CIRCLE DEBUG: Flight path has ${flightPoints.length} points`);
      }

      // Add the polyline to the map
      flightPath.setMap(mapRef.current);
      
      console.log(`Created polyline with ${flightPoints.length} points`);
      
      try {
        // Update starting index field if it exists
        const startingIndexField = document.getElementById("in_startingIndex");
        if (startingIndexField) {
          startingIndexField.value = flagCount;
        }
      } catch (error) {
        console.error('Error updating starting index field:', error);
      }

      // Store the polyline in the map's lines array
      if (!mapRef.current.lines) {
        mapRef.current.lines = [];
      }
      mapRef.current.lines.push(flightPath);
      redrawFlightPaths();
      
      console.log(`Successfully rendered ${generatedPoints.length} waypoints and flight path`);
      return generatedPoints;
    } catch (error) {
      console.error('Error generating waypoints:', error);
      
      // Log more detailed error information
      if (error.response) {
        // The request was made and the server responded with a status code
        // that falls out of the range of 2xx
        console.error('Response data:', error.response.data);
        console.error('Response status:', error.response.status);
        console.error('Response headers:', error.response.headers);
        throw error.response.data || 'Server error generating waypoints';
      } else if (error.request) {
        // The request was made but no response was received
        console.error('No response received:', error.request);
        throw new Error('No response from server. Please check your connection.');
      } else {
        // Something happened in setting up the request that triggered an Error
        console.error('Error message:', error.message);
        throw error;
      }
    }
  }, [mapRef, genInfoWindowRef, setSelectedShape, redrawFlightPaths]);

  /**
   * Generate KML file for waypoints
   */
  const generateKml = useCallback(async (downloadLinkRef) => {
    if (!mapRef.current || !mapRef.current.flags || mapRef.current.flags.length === 0) {
      alert('No waypoints to export');
      return;
    }

    // Create a clean request object with only the properties we need
    const requestData = {
      FlyToWaylineMode: "safely",
      FinishAction: "0",
      ExitOnRCLost: "executeLostAction",
      ExecuteRCLostAction: "goBack",
      GlobalTransitionalSpeed: 2.5,
      DroneInfo: {
        DroneEnumValue: 1,
        DroneSubEnumValue: 1
      },
      Waypoints: [],
      ActionGroups: []
    };

    // Clean waypoints data to avoid circular references
    requestData.Waypoints = mapRef.current.flags.map((wp, index) => {
      return {
        Index: index,
        Latitude: Number(wp.lat || 0),
        Longitude: Number(wp.lng || 0),
        ExecuteHeight: Number(wp.altitude || 60),
        WaypointSpeed: Number(wp.speed || 2.5),
        WaypointHeadingMode: "smoothTransition",
        WaypointHeadingAngle: Number(wp.heading || 0),
        WaypointHeadingPathMode: "followBadArc",
        WaypointTurnMode: "toPointAndStopWithContinuityCurvature",
        WaypointTurnDampingDist: "0",
        Action: String(wp.action || "noAction")
      };
    });

    // Verify the data can be serialized
    try {
      const serialized = JSON.stringify(requestData);
      console.log(`KML request prepared with ${requestData.Waypoints.length} waypoints`);
    } catch (error) {
      console.error('Error serializing KML request:', error);
      alert('Failed to prepare KML data - please try again');
      return;
    }

    try {
      const response = await axios.post(`${apiBaseUrl}/api/KMZ/generate`, requestData, {
        responseType: 'blob'
      });

      // Create a URL for the blob
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = downloadLinkRef.current;
      if (link) {
        link.href = url;
        link.setAttribute('download', 'generated.kml');
        link.click();
        window.URL.revokeObjectURL(url);
      }
    } catch (error) {
      console.error('Error generating KML:', error);
      alert('Error generating KML file. Check console for details.');
    }
  }, [mapRef, apiBaseUrl]);

  return {
    generateWaypointsFromAPI,
    generateKml
  };
}; 