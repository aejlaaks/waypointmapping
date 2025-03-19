using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Service for generating waypoints for polyline shapes
    /// </summary>
    public class PolylineShapeService : IShapeService
    {
        private readonly IGeometryService _geometryService;
        private readonly ILogger<PolylineShapeService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineShapeService"/> class.
        /// </summary>
        /// <param name="geometryService">Geometry service for shape operations</param>
        /// <param name="logger">Logger for the polyline shape service</param>
        public PolylineShapeService(IGeometryService geometryService, ILogger<PolylineShapeService> logger)
        {
            _geometryService = geometryService;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool CanHandleShapeType(string shapeType)
        {
            return shapeType?.ToLower() == ShapeTypes.Polyline;
        }

        /// <inheritdoc />
        public List<Waypoint> GenerateWaypoints(ShapeData shape, WaypointParameters parameters)
        {
            _logger.LogInformation("Generating polyline waypoints with parameters: Altitude={Altitude}, Speed={Speed}, UseEndpointsOnly={UseEndpointsOnly}",
                parameters.Altitude, parameters.Speed, parameters.UseEndpointsOnly);
                
            if (shape == null || shape.Coordinates == null || shape.Coordinates.Count < 2)
            {
                return new List<Waypoint>();
            }
            
            // Validate and set default parameters if needed
            if (parameters.Speed <= 0)
            {
                parameters.Speed = 5.0; // Default speed in m/s
                _logger.LogWarning("Invalid speed value. Using default speed of {Speed} m/s", parameters.Speed);
            }
            
            if (parameters.Altitude <= 0)
            {
                parameters.Altitude = 50.0; // Default altitude in meters
                _logger.LogWarning("Invalid altitude value. Using default altitude of {Altitude} m", parameters.Altitude);
            }
            
            if (parameters.LineSpacing <= 0)
            {
                parameters.LineSpacing = 20.0; // Default line spacing in meters
                _logger.LogWarning("Invalid line spacing value. Using default spacing of {LineSpacing} m", parameters.LineSpacing);
            }
            
            var waypoints = new List<Waypoint>();
            var coordinates = shape.Coordinates;
            
            _logger.LogInformation("Polyline has {Count} coordinate points", coordinates.Count);
            
            int id = parameters.StartingIndex;

            // Calculate camera FOV and ground coverage based on camera parameters (if provided)
            double effectivePhotoInterval = parameters.PhotoInterval;
            
            // If we have camera parameters available, calculate optimal spacing and photo interval
            if (parameters.SensorWidth > 0 && parameters.SensorHeight > 0 && parameters.FocalLength > 0 && parameters.Overlap > 0)
            {
                // Calculate horizontal and vertical FOV in radians
                double fovH = 2 * Math.Atan(parameters.SensorWidth / (2 * parameters.FocalLength));
                double fovV = 2 * Math.Atan(parameters.SensorHeight / (2 * parameters.FocalLength));
                
                // Calculate ground coverage dimensions
                double groundWidth = 2 * parameters.Altitude * Math.Tan(fovH / 2);
                double groundHeight = 2 * parameters.Altitude * Math.Tan(fovV / 2);
                
                // Calculate distance between photos
                double distanceBetweenPhotos = groundHeight * (1 - parameters.Overlap / 100.0);
                
                // If speed is not manually set, calculate it from photo interval
                if (!parameters.ManualSpeedSet && parameters.PhotoInterval > 0)
                {
                    // Speed = distance / time
                    double calculatedSpeed = distanceBetweenPhotos / parameters.PhotoInterval;
                    _logger.LogInformation("Calculated optimal speed: {Speed} m/s based on photo interval", calculatedSpeed);
                }
                else
                {
                    // If speed is manually set, adjust photo interval accordingly
                    effectivePhotoInterval = distanceBetweenPhotos / parameters.Speed;
                }
                
                _logger.LogInformation("Camera calculations: Ground Width={Width}m, Height={Height}m, Photo Distance={PhotoDistance}m",
                    groundWidth, groundHeight, distanceBetweenPhotos);
            }
            else if (effectivePhotoInterval <= 0)
            {
                // If no photo interval is set, use a default value based on speed
                // This ensures we get a reasonable number of waypoints even without camera parameters
                effectivePhotoInterval = 2.0; // Default 2 seconds between photos
                _logger.LogInformation("Using default photo interval of {Interval} seconds", effectivePhotoInterval);
            }
            
            // Determine if this is a closed polyline (first and last points are the same)
            bool isClosedPolyline = coordinates.Count > 2 && 
                      coordinates[0].Lat == coordinates[coordinates.Count - 1].Lat &&
                      coordinates[0].Lng == coordinates[coordinates.Count - 1].Lng;
            
            _logger.LogInformation("Polyline is {Type}", isClosedPolyline ? "closed (polygon)" : "open");
            
            // For closed polylines, we'll generate a zigzag pattern like rectangular area coverage
            if (isClosedPolyline)
            {
                // Determine the area boundaries (min and max lat/lng)
                double minLat = coordinates.Min(c => c.Lat);
                double maxLat = coordinates.Max(c => c.Lat);
                double minLng = coordinates.Min(c => c.Lng);
                double maxLng = coordinates.Max(c => c.Lng);
                
                // Calculate line spacing based on photo parameters
                double lineSpacing = parameters.LineSpacing;
                
                // Recalculate based on camera parameters if available
                if (parameters.SensorWidth > 0 && parameters.FocalLength > 0 && parameters.Overlap > 0)
                {
                    double fovH = 2 * Math.Atan(parameters.SensorWidth / (2 * parameters.FocalLength));
                    double groundWidth = 2 * parameters.Altitude * Math.Tan(fovH / 2);
                    lineSpacing = groundWidth * (1 - parameters.Overlap / 100.0);
                }
                
                if (parameters.IsNorthSouth)
                {
                    // North-South direction
                    double lngDistanceInDegrees = lineSpacing / (111320.0 * Math.Cos(minLat * Math.PI / 180.0));
                    bool isEvenLine = true;
                    
                    for (double lng = minLng; lng <= maxLng; lng += lngDistanceInDegrees)
                    {
                        List<Waypoint> lineWaypoints = new List<Waypoint>();
                        
                        // Define start and end points for this line through the entire bounding box
                        double boundingStartLat = minLat;
                        double boundingEndLat = maxLat;
                        double heading = isEvenLine ? 0 : 180; // North or South
                        
                        // Find the intersections with the polygon boundaries
                        var intersections = FindPolygonIntersections(coordinates, boundingStartLat, lng, boundingEndLat, lng);
                        
                        // Sort intersections by latitude
                        intersections.Sort((a, b) => a.Lat.CompareTo(b.Lat));
                        
                        // Process pairs of intersections (entries and exits from the polygon)
                        for (int i = 0; i < intersections.Count; i += 2)
                        {
                            if (i + 1 >= intersections.Count) break;
                            
                            // Get the entry and exit points for this segment within the polygon
                            double segmentStartLat = intersections[i].Lat;
                            double segmentEndLat = intersections[i + 1].Lat;
                            
                            // Adjust direction based on even/odd line
                            double startLat = isEvenLine ? segmentStartLat : segmentEndLat;
                            double endLat = isEvenLine ? segmentEndLat : segmentStartLat;
                            
                            if (parameters.UseEndpointsOnly)
                            {
                                // For useEndpointsOnly=true, only add waypoints at the entry and exit points
                                lineWaypoints.Add(new Waypoint(
                                    id++,
                                    startLat,
                                    lng,
                                    parameters.Altitude,
                                    parameters.Speed,
                                    parameters.Action
                                ) { Heading = heading });
                                
                                lineWaypoints.Add(new Waypoint(
                                    id++,
                                    endLat,
                                    lng,
                                    parameters.Altitude,
                                    parameters.Speed,
                                    parameters.Action
                                ) { Heading = heading });
                            }
                            else
                            {
                                // Calculate distance of this line segment within the polygon
                                double lineDistanceMeters = _geometryService.CalculateDistance(startLat, lng, endLat, lng);
                                
                                // Calculate waypoints based on photo interval
                                double distancePerPhoto = parameters.Speed * effectivePhotoInterval;
                                int numWaypoints = Math.Max(2, (int)(lineDistanceMeters / distancePerPhoto) + 1);
                                
                                for (int j = 0; j < numWaypoints; j++)
                                {
                                    double fraction = (double)j / (numWaypoints - 1);
                                    double lat = startLat + fraction * (endLat - startLat);
                                    
                                    lineWaypoints.Add(new Waypoint(
                                        id++,
                                        lat,
                                        lng,
                                        parameters.Altitude,
                                        parameters.Speed,
                                        parameters.Action
                                    ) { Heading = heading });
                                }
                            }
                        }
                        
                        waypoints.AddRange(lineWaypoints);
                        isEvenLine = !isEvenLine; // Alternate direction for next line
                    }
                }
                else
                {
                    // East-West direction
                    double latDistanceInDegrees = lineSpacing / 111320.0;
                    bool isEvenLine = true;
                    
                    for (double lat = minLat; lat <= maxLat; lat += latDistanceInDegrees)
                    {
                        List<Waypoint> lineWaypoints = new List<Waypoint>();
                        
                        // Define start and end points for this line through the entire bounding box
                        double boundingStartLng = minLng;
                        double boundingEndLng = maxLng;
                        double heading = isEvenLine ? 90 : 270; // East or West
                        
                        // Find the intersections with the polygon boundaries
                        var intersections = FindPolygonIntersections(coordinates, lat, boundingStartLng, lat, boundingEndLng);
                        
                        // Sort intersections by longitude
                        intersections.Sort((a, b) => a.Lng.CompareTo(b.Lng));
                        
                        // Process pairs of intersections (entries and exits from the polygon)
                        for (int i = 0; i < intersections.Count; i += 2)
                        {
                            if (i + 1 >= intersections.Count) break;
                            
                            // Get the entry and exit points for this segment within the polygon
                            double segmentStartLng = intersections[i].Lng;
                            double segmentEndLng = intersections[i + 1].Lng;
                            
                            // Adjust direction based on even/odd line
                            double startLng = isEvenLine ? segmentStartLng : segmentEndLng;
                            double endLng = isEvenLine ? segmentEndLng : segmentStartLng;
                            
                            if (parameters.UseEndpointsOnly)
                            {
                                // For useEndpointsOnly=true, only add waypoints at the entry and exit points
                                lineWaypoints.Add(new Waypoint(
                                    id++,
                                    lat,
                                    startLng,
                                    parameters.Altitude,
                                    parameters.Speed,
                                    parameters.Action
                                ) { Heading = heading });
                                
                                lineWaypoints.Add(new Waypoint(
                                    id++,
                                    lat,
                                    endLng,
                                    parameters.Altitude,
                                    parameters.Speed,
                                    parameters.Action
                                ) { Heading = heading });
                            }
                            else
                            {
                                // Calculate distance of this line segment within the polygon
                                double lineDistanceMeters = _geometryService.CalculateDistance(lat, startLng, lat, endLng);
                                
                                // Calculate waypoints based on photo interval
                                double distancePerPhoto = parameters.Speed * effectivePhotoInterval;
                                int numWaypoints = Math.Max(2, (int)(lineDistanceMeters / distancePerPhoto) + 1);
                                
                                for (int j = 0; j < numWaypoints; j++)
                                {
                                    double fraction = (double)j / (numWaypoints - 1);
                                    double lng = startLng + fraction * (endLng - startLng);
                                    
                                    lineWaypoints.Add(new Waypoint(
                                        id++,
                                        lat,
                                        lng,
                                        parameters.Altitude,
                                        parameters.Speed,
                                        parameters.Action
                                    ) { Heading = heading });
                                }
                            }
                        }
                        
                        waypoints.AddRange(lineWaypoints);
                        isEvenLine = !isEvenLine; // Alternate direction for next line
                    }
                }
            }
            else
            {
                // For open polylines, follow the path directly
                for (int i = 0; i < coordinates.Count; i++)
                {
                    // Add waypoints at each vertex for both useEndpointsOnly true and false
                    var waypoint = new Waypoint(
                        id++,
                        coordinates[i].Lat,
                        coordinates[i].Lng,
                        parameters.Altitude,
                        parameters.Speed,
                        parameters.Action
                    );
                    
                    // Calculate heading for this waypoint
                    if (i < coordinates.Count - 1)
                    {
                        // If not the last point, heading is toward the next point
                        double headingToNext = CalculateHeading(
                            coordinates[i].Lat, coordinates[i].Lng,
                            coordinates[i + 1].Lat, coordinates[i + 1].Lng);
                        waypoint.Heading = headingToNext;
                    }
                    else if (i > 0)
                    {
                        // If last point, heading is the same as the previous segment
                        double headingFromPrevious = CalculateHeading(
                            coordinates[i - 1].Lat, coordinates[i - 1].Lng,
                            coordinates[i].Lat, coordinates[i].Lng);
                        waypoint.Heading = headingFromPrevious;
                    }
                    
                    waypoints.Add(waypoint);
                    
                    // Add intermediate waypoints along segments
                    // For open polylines, we add intermediate points only when useEndpointsOnly is false
                    if (i < coordinates.Count - 1 && !parameters.UseEndpointsOnly)
                    {
                        // Calculate distance between this vertex and the next
                        double segmentDistance = _geometryService.CalculateDistance(
                            coordinates[i].Lat, coordinates[i].Lng,
                            coordinates[i + 1].Lat, coordinates[i + 1].Lng);
                        
                        // Calculate how many waypoints to add based on speed and photo interval
                        double distancePerPhoto = parameters.Speed * effectivePhotoInterval;
                        
                        // Use photo interval to determine spacing, but ensure we always have some points
                        // along segments for open polylines
                        int additionalPoints;
                        
                        if (distancePerPhoto > 0)
                        {
                            additionalPoints = Math.Max(1, (int)(segmentDistance / distancePerPhoto) - 1);
                        }
                        else
                        {
                            // If interval calculation fails, add at least some points based on segment length
                            double minPointsPerMeter = 0.05; // At least 1 point every 20 meters
                            additionalPoints = Math.Max(1, (int)(segmentDistance * minPointsPerMeter));
                        }
                        
                        _logger.LogInformation("Segment {Index} length: {Length}m, Photo interval: {Interval}m, Adding {Count} intermediate points", 
                            i, segmentDistance, distancePerPhoto, additionalPoints);
                        
                        // Add intermediate waypoints along this segment
                        for (int j = 1; j <= additionalPoints; j++)
                        {
                            // Calculate position as a fraction along the segment
                            double fraction = j / (double)(additionalPoints + 1);
                            
                            // Linear interpolation between points
                            double lat = coordinates[i].Lat + fraction * (coordinates[i + 1].Lat - coordinates[i].Lat);
                            double lng = coordinates[i].Lng + fraction * (coordinates[i + 1].Lng - coordinates[i].Lng);
                            
                            // Add intermediate waypoint
                            var intermediateWaypoint = new Waypoint(
                                id++,
                                lat,
                                lng,
                                parameters.Altitude,
                                parameters.Speed,
                                parameters.Action
                            );
                            
                            // Heading is the same as the segment
                            double heading = CalculateHeading(
                                coordinates[i].Lat, coordinates[i].Lng,
                                coordinates[i + 1].Lat, coordinates[i + 1].Lng);
                            intermediateWaypoint.Heading = heading;
                            
                            waypoints.Add(intermediateWaypoint);
                        }
                    }
                }
            }
            
            // Log the waypoint count for debugging
            _logger.LogInformation("Generated {Count} waypoints for polyline with UseEndpointsOnly={UseEndpointsOnly}", 
                waypoints.Count, parameters.UseEndpointsOnly);
            
            return waypoints;
        }

        // Helper method to calculate heading from point 1 to point 2
        private double CalculateHeading(double lat1, double lng1, double lat2, double lng2)
        {
            // Convert to radians
            double lat1Rad = lat1 * Math.PI / 180.0;
            double lng1Rad = lng1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            double lng2Rad = lng2 * Math.PI / 180.0;
            
            // Calculate heading
            double y = Math.Sin(lng2Rad - lng1Rad) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                      Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(lng2Rad - lng1Rad);
            double heading = Math.Atan2(y, x) * 180.0 / Math.PI;
            
            // Normalize to 0-360
            return (heading + 360) % 360;
        }

        // Helper method to check if a point is inside a polygon or polyline
        private bool IsPointInPolygon(List<Coordinate> polygon, double lat, double lng)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Lat > lat) != (polygon[j].Lat > lat)) &&
                    (lng < (polygon[j].Lng - polygon[i].Lng) * (lat - polygon[i].Lat) / (polygon[j].Lat - polygon[i].Lat) + polygon[i].Lng))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        // Helper method to check if a line intersects a polygon
        private bool DoesLineIntersectPolygon(List<Coordinate> polygon, double lat1, double lng1, double lat2, double lng2)
        {
            // A simple check - if either endpoint is inside the polygon, the line intersects
            if (IsPointInPolygon(polygon, lat1, lng1) || IsPointInPolygon(polygon, lat2, lng2))
            {
                return true;
            }
            
            // Check if the line crosses any polygon edge
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (DoLineSegmentsIntersect(
                    lat1, lng1, lat2, lng2,
                    polygon[i].Lat, polygon[i].Lng, polygon[j].Lat, polygon[j].Lng))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // Helper method to check if two line segments intersect
        private bool DoLineSegmentsIntersect(double lat1, double lng1, double lat2, double lng2,
                                            double lat3, double lng3, double lat4, double lng4)
        {
            // Calculate the orientation of three points
            int orientation1 = Orientation(lat1, lng1, lat2, lng2, lat3, lng3);
            int orientation2 = Orientation(lat1, lng1, lat2, lng2, lat4, lng4);
            int orientation3 = Orientation(lat3, lng3, lat4, lng4, lat1, lng1);
            int orientation4 = Orientation(lat3, lng3, lat4, lng4, lat2, lng2);
            
            // General case
            if (orientation1 != orientation2 && orientation3 != orientation4)
            {
                return true;
            }
            
            // Special cases
            if (orientation1 == 0 && OnSegment(lat1, lng1, lat3, lng3, lat2, lng2)) return true;
            if (orientation2 == 0 && OnSegment(lat1, lng1, lat4, lng4, lat2, lng2)) return true;
            if (orientation3 == 0 && OnSegment(lat3, lng3, lat1, lng1, lat4, lng4)) return true;
            if (orientation4 == 0 && OnSegment(lat3, lng3, lat2, lng2, lat4, lng4)) return true;
            
            return false;
        }
        
        // Helper method to check if point q lies on segment pr
        private bool OnSegment(double pLat, double pLng, double qLat, double qLng, double rLat, double rLng)
        {
            return qLng <= Math.Max(pLng, rLng) && qLng >= Math.Min(pLng, rLng) &&
                   qLat <= Math.Max(pLat, rLat) && qLat >= Math.Min(pLat, rLat);
        }
        
        // Helper method to determine orientation of triplet (p, q, r)
        private int Orientation(double pLat, double pLng, double qLat, double qLng, double rLat, double rLng)
        {
            double val = (qLat - pLat) * (rLng - qLng) - (qLng - pLng) * (rLat - qLat);
            
            if (Math.Abs(val) < 1e-10) return 0;  // Collinear
            return (val > 0) ? 1 : 2;             // Clockwise or Counterclockwise
        }

        // Helper method to find polygon intersections
        private List<Coordinate> FindPolygonIntersections(List<Coordinate> polygon, double lat1, double lng1, double lat2, double lng2)
        {
            var intersections = new List<Coordinate>();
            
            // First check if start and end points are inside the polygon
            bool startInside = IsPointInPolygon(polygon, lat1, lng1);
            bool endInside = IsPointInPolygon(polygon, lat2, lng2);
            
            // If both points are inside, we should add them both (entry and exit)
            if (startInside && endInside)
            {
                intersections.Add(new Coordinate { Lat = lat1, Lng = lng1 });
                intersections.Add(new Coordinate { Lat = lat2, Lng = lng2 });
                return intersections;
            }
            
            // Find intersections with all polygon edges
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (DoLineSegmentsIntersect(
                    lat1, lng1, lat2, lng2,
                    polygon[i].Lat, polygon[i].Lng, polygon[j].Lat, polygon[j].Lng))
                {
                    var intersection = FindIntersection(
                        lat1, lng1, lat2, lng2,
                        polygon[i].Lat, polygon[i].Lng, polygon[j].Lat, polygon[j].Lng);
                    
                    if (intersection != null)
                    {
                        // Avoid duplicate intersections (can happen at vertices)
                        bool isDuplicate = false;
                        foreach (var existing in intersections)
                        {
                            if (Math.Abs(existing.Lat - intersection.Lat) < 1e-10 && 
                                Math.Abs(existing.Lng - intersection.Lng) < 1e-10)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                        
                        if (!isDuplicate)
                        {
                            intersections.Add(intersection);
                        }
                    }
                }
            }
            
            // If only start point is inside, add it as first intersection
            if (startInside && intersections.Count > 0)
            {
                intersections.Insert(0, new Coordinate { Lat = lat1, Lng = lng1 });
            }
            
            // If only end point is inside, add it as last intersection
            if (endInside && intersections.Count > 0)
            {
                intersections.Add(new Coordinate { Lat = lat2, Lng = lng2 });
            }
            
            // Ensure we have an even number of intersections for entry/exit pairs
            if (intersections.Count % 2 == 1)
            {
                _logger.LogWarning("Odd number of polygon intersections found ({Count}). This may indicate an error in the intersection calculation.", 
                    intersections.Count);
                
                // If we have an odd number, we'll need to add one more intersection
                // This can happen in edge cases like tangent lines
                if (startInside)
                {
                    // If start is inside, add the start point if not already added
                    bool hasStart = false;
                    foreach (var point in intersections)
                    {
                        if (Math.Abs(point.Lat - lat1) < 1e-10 && Math.Abs(point.Lng - lng1) < 1e-10)
                        {
                            hasStart = true;
                            break;
                        }
                    }
                    
                    if (!hasStart)
                    {
                        intersections.Insert(0, new Coordinate { Lat = lat1, Lng = lng1 });
                    }
                }
                else if (endInside)
                {
                    // If end is inside, add the end point if not already added
                    bool hasEnd = false;
                    foreach (var point in intersections)
                    {
                        if (Math.Abs(point.Lat - lat2) < 1e-10 && Math.Abs(point.Lng - lng2) < 1e-10)
                        {
                            hasEnd = true;
                            break;
                        }
                    }
                    
                    if (!hasEnd)
                    {
                        intersections.Add(new Coordinate { Lat = lat2, Lng = lng2 });
                    }
                }
                else
                {
                    // If neither point is inside but we still have an odd count, it's likely
                    // that the line is tangent to the polygon at some point
                    // In this case, we'll duplicate the intersection to make it an entry and exit
                    if (intersections.Count > 0)
                    {
                        Coordinate lastIntersection = intersections[intersections.Count - 1];
                        intersections.Add(new Coordinate { Lat = lastIntersection.Lat, Lng = lastIntersection.Lng });
                    }
                }
            }
            
            return intersections;
        }

        // Helper method to find the intersection of two lines
        private Coordinate FindIntersection(double lat1, double lng1, double lat2, double lng2,
                                           double lat3, double lng3, double lat4, double lng4)
        {
            double x1 = lat1;
            double y1 = lng1;
            double x2 = lat2;
            double y2 = lng2;
            double x3 = lat3;
            double y3 = lng3;
            double x4 = lat4;
            double y4 = lng4;

            double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denom) < 1e-10)
            {
                // Lines are nearly parallel or coincident
                // Check if they're coincident by checking if one endpoint lies on the other line
                if (OnSegment(x1, y1, x3, y3, x2, y2) || 
                    OnSegment(x1, y1, x4, y4, x2, y2) ||
                    OnSegment(x3, y3, x1, y1, x4, y4) || 
                    OnSegment(x3, y3, x2, y2, x4, y4))
                {
                    // If lines are coincident, we need to determine which endpoint is the intersection
                    // We'll use the midpoint between any overlapping segments as the intersection
                    double[] xPoints = new double[] { x1, x2, x3, x4 };
                    double[] yPoints = new double[] { y1, y2, y3, y4 };
                    Array.Sort(xPoints);
                    Array.Sort(yPoints);
                    
                    // Use the middle two points (which should be the overlapping segment)
                    return new Coordinate { Lat = (xPoints[1] + xPoints[2]) / 2, Lng = (yPoints[1] + yPoints[2]) / 2 };
                }
                
                return null;
            }

            double ua = ((x3 - x4) * (y1 - y3) - (y3 - y4) * (x1 - x3)) / denom;
            double ub = ((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

            // Use slightly relaxed bounds checking to handle numerical precision issues
            const double EPSILON = 1e-10;
            if (ua >= -EPSILON && ua <= 1 + EPSILON && ub >= -EPSILON && ub <= 1 + EPSILON)
            {
                // Clamp values to handle numerical precision issues
                ua = Math.Max(0, Math.Min(1, ua));
                
                double ix = x1 + ua * (x2 - x1);
                double iy = y1 + ua * (y2 - y1);
                return new Coordinate { Lat = ix, Lng = iy };
            }

            return null;
        }
    }
} 