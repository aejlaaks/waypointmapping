import React from 'react';
import '../App.css';

/**
 * Component for map toolbar with drawing controls
 */
const MapToolbar = ({ 
  onStopDrawing, 
  onGenerateWaypoints, 
  onGenerateKml,
  onDrawRectangle,
  onDrawCircle,
  onDrawPolyline,
  onClearShapes,
  startingIndex = 1
}) => {
  // Debug handler function to log button clicks
  const handleButtonClick = (action, handler) => {
    console.log(`Button clicked: ${action}`);
    if (typeof handler === 'function') {
      handler();
    } else {
      console.error(`Handler for ${action} is not a function:`, handler);
    }
  };

  return (
    <div className="button-container">
      <div className="button-group">
        <h4 className="button-group-title">Drawing Tools</h4>
        <button className="draw-rectangle-button" onClick={() => handleButtonClick('Draw Rectangle', onDrawRectangle)}>
          Rectangle
        </button>
        <button className="draw-rectangle-button" onClick={() => handleButtonClick('Draw Circle', onDrawCircle)}>
          Circle
        </button>
        <button className="draw-rectangle-button" onClick={() => handleButtonClick('Draw Polyline', onDrawPolyline)}>
          Polyline
        </button>
        <button className="stop-drawing-button" onClick={() => handleButtonClick('Stop Drawing', onStopDrawing)}>
          Stop Drawing
        </button>
      </div>
      
      <div className="button-group">
        <h4 className="button-group-title">Actions</h4>
        <input type="hidden" id="in_startingIndex" value={startingIndex} />
        <button className="generate-waypoints-button" onClick={() => handleButtonClick('Generate Waypoints', onGenerateWaypoints)}>
          Generate Waypoints
        </button>
        <button className="generate-kml-button" onClick={() => handleButtonClick('Export KML', onGenerateKml)}>
          Export KML
        </button>
        <button className="clear-shapes-button" onClick={() => handleButtonClick('Clear All', onClearShapes)}>
          Clear All
        </button>
      </div>
    </div>
  );
};

export default MapToolbar; 