# DroneKartta - Drone Flight Path Planning Application

DroneKartta is a web-based application for planning and visualizing drone flight paths. It allows users to create waypoints based on different geometric shapes (rectangles, circles, polygons, and polylines) and generate KML files for drone flight missions.

## Features

- **Interactive Map Interface**: Draw shapes on the map to define flight areas
- **Multiple Shape Types**: Support for rectangles, circles, polygons, and polylines
- **Waypoint Generation**: Automatically generate waypoints based on shape and flight parameters
- **Flight Parameter Customization**: Configure altitude, speed, camera settings, and more
- **Waypoint Editing**: Edit individual waypoint properties including altitude, speed, and heading
- **KML Export**: Generate and download KML files compatible with drone flight controllers
- **Responsive Design**: Works on desktop and mobile devices

## Recent Updates

### Map Control Debugging Improvements

The application now includes enhanced debugging and improved map control functionality with the following updates:

1. **Drawing Manager Initialization**:
   - Fixed initialization sequence for the Google Maps Drawing Manager
   - Implemented proper reference tracking for drawing tools
   - Added logging for map and drawing manager loading events

2. **Button Event Handlers**:
   - Implemented callback-based button handlers with proper dependency tracking
   - Added debugging logs for all control actions
   - Improved error handling for drawing operations

3. **Code Structure Improvements**:
   - Separated concerns for drawing operations and waypoint generation
   - Implemented proper state management for map objects
   - Enhanced visualization of drawing tools state

4. **Developer Tools**:
   - Added extensive console logging for debugging drawing operations
   - Improved error reporting for map operations
   - Added state tracking for drawing manager references

### Enhanced Polyline Waypoint Visualization

The application now includes improved support for polyline-based flight paths with the following enhancements:

1. **Visual Waypoint Differentiation**:
   - Vertex waypoints (at polyline corners) displayed with distinct styling
   - Intermediate waypoints shown with different colors and sizes
   - Directional indicators showing waypoint heading

2. **User Interface Improvements**:
   - Added shape type selector with dedicated polyline option
   - Contextual help for polyline-specific settings
   - Automatic disabling of irrelevant controls when working with polylines
   - Toast notifications for success and error feedback

3. **Waypoint Management**:
   - Enhanced waypoint list with color-coding for different waypoint types
   - Visual indicators for start, end, and vertex points
   - Improved waypoint info display with heading visualization

4. **Theming and Responsiveness**:
   - Dark/light mode toggle for improved visibility in different conditions
   - Enhanced mobile responsiveness for field use
   - Improved form layout and control organization

### UI Screenshots

![polylineWithout](https://github.com/user-attachments/assets/6c9497c7-b12b-410f-bc7c-480530b04c5f)
![rectangleWaypoints](https://github.com/user-attachments/assets/49d0f90d-e274-42dd-a165-d40f6d71539b)
![rectangleOnlyEndpoint](https://github.com/user-attachments/assets/8712e8ca-dbca-40d7-ada2-7a4c0e506b1a)
![polylineAll](https://github.com/user-attachments/assets/2529f983-f6c8-4da9-9756-2612bfc97b24)
![CircleMap](https://github.com/user-attachments/assets/e985a00b-4831-42d1-87cc-ff78faf9626a)

## Technical Implementation

### Polyline Waypoint Generation

The application implements a specialized algorithm for generating waypoints along polylines:

1. **Segment-Based Processing**:
   - Each polyline segment (between two vertices) is processed individually
   - Waypoints are generated along each segment based on distance and photo interval settings

2. **Configurable Density**:
   - When "Use Endpoints Only" is enabled: waypoints are placed only at polyline vertices
   - When disabled: intermediate waypoints are calculated based on the desired photo interval and flight speed
   - The formula calculates the distance between points as: `photoInterval * speed`

3. **Heading Calculation**:
   - Each waypoint stores a heading value calculated based on the direction of the segment
   - For vertex waypoints, the heading is determined based on the incoming and outgoing segments
   - The heading calculation uses the arctangent of the longitude and latitude differences

4. **Optimization**:
   - The algorithm avoids duplicate waypoints at segment junctions
   - Distance calculations use the Haversine formula for accurate Earth-surface distances
   - Vertex detection includes both explicit vertices and points with significant heading changes

### Performance Considerations

The waypoint generation algorithm is optimized for:

- **Memory Efficiency**: Points are generated on-demand based on segments
- **CPU Efficiency**: Calculations are simplified for standard use cases while maintaining accuracy
- **Responsive UI**: Map rendering maintains 60fps even with complex flight paths

## Getting Started

### Prerequisites

- Node.js (v14 or higher)
- .NET Core SDK (v6.0 or higher)

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/DroneKartta.git
   cd DroneKartta
   ```

2. Install client dependencies:
   ```
   cd ReactApp2.Client
   npm install
   ```

3. Install server dependencies:
   ```
   cd ../ReactApp2.Server
   dotnet restore
   ```

### Running the Application

1. Start the backend server:
   ```
   cd ReactApp2.Server
   dotnet run
   ```

2. Start the frontend development server:
   ```
   cd ReactApp2.Client
   npm run dev
   ```

3. Open your browser and navigate to `http://localhost:5173` (or the port shown in your terminal)

## Usage Guide

### Creating a Flight Path

1. Use the drawing tools on the map to create a shape:
   - Rectangle: Define area with two corner points
   - Circle: Define center point and radius
   - Polygon: Define multiple vertices to create a custom shape
   - Polyline: Create a path with multiple connected points

2. Configure flight parameters:
   - Altitude: Set the flight altitude in meters
   - Camera settings: Adjust focal length, sensor dimensions, etc.
   - Speed and intervals: Set flight speed and photo intervals

3. For polyline paths:
   - Toggle "Use Endpoints Only" to create waypoints only at polyline vertices
   - Disable this option to generate intermediate waypoints along the path

4. Click "Generate Waypoints" to create the flight plan

### Editing Waypoints

- Click on a waypoint to view and edit its properties
- Drag waypoints to adjust their position
- Edit altitude, speed, heading, and actions for each waypoint
- Delete unwanted waypoints

### Exporting Flight Plan

- Click "Download KML" to save the flight plan in KML format
- Import the KML file into your drone's flight controller software

## Project Structure

The DroneKartta solution consists of the following projects:

- **ReactApp2.Client**: Frontend React application with TypeScript
- **ReactApp2.Server**: Backend .NET API with domain logic
- **KarttaBackendTest**: Test project for backend services

The backend codebase uses the namespace `KarttaBackEnd2.Server` for consistency with legacy code.

## Testing

The application includes comprehensive tests for both frontend and backend components:

```
# Run backend tests
cd KarttaBackendTest
dotnet test

# Run frontend tests
cd ReactApp2.Client
npm test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Uses Google Maps API for map visualization
- Built with React and .NET Core
- Styling based on Bootstrap framework

## Contributing

Contributions to DroneKartta are welcome! Here's how you can help improve the application:

### Areas for Improvement

1. **Enhanced Path Planning**:
   - Add support for more complex path types (spirals, grid patterns)
   - Implement obstacle avoidance algorithms
   - Add terrain-following capabilities

2. **UI Enhancements**:
   - Improve mobile responsiveness
   - Add support for more localization options
   - Enhance accessibility features

3. **Additional Export Formats**:
   - Support for DJI flight planning formats
   - Integration with additional drone manufacturers
   - Mission planning format converters

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Implement your changes
4. Write tests for your implementation
5. Commit your changes (`git commit -m 'Add some amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

Please ensure your code adheres to the existing style conventions and includes appropriate tests. 
