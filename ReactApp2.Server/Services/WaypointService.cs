using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using SharpKml.Dom;
using Waypoint = KarttaBackEnd2.Server.Models.Waypoint;

namespace KarttaBackEnd2.Server.Services
{
    /// <summary>
    /// Legacy implementation of IWaypointService maintained for backward compatibility
    /// </summary>
    public class WaypointService : IWaypointService
    {
        private readonly ILogger<WaypointService> _logger;

        public WaypointService(ILogger<WaypointService> logger)
        {
            _logger = logger;
        }

        // Legacy implementation - this method will be removed in the future
        public async Task<List<Waypoint>> GenerateWaypointsAsync(
            string action,
            int unitType_in,
            double altitude,
            double speed,
            int angle, // kept for backward compatibility but no longer used
            double in_distance,
            List<Coordinate> bounds,
            string boundsType,
            int in_startingIndex,
            double photoInterval = 3,
            bool useEndpointsOnly = false,
            bool isNorthSouth = false
        )
        {
            _logger.LogInformation("Using legacy waypoint generation method");
            var waypoints = new List<Waypoint>();
            int id = in_startingIndex;
            action = string.IsNullOrEmpty(action) ? "takePhoto" : action;

            if (boundsType == "rectangle" || boundsType == "polygon")
            {
                waypoints.AddRange(GenerateWaypointsForRectangleOrPolygon(bounds, altitude, speed, angle, in_distance, useEndpointsOnly, isNorthSouth, ref id, action));
            }
            else if (boundsType == "circle")
            {
                foreach (var bound in bounds)
                {
                    double centerLat = bound.Lat;
                    double centerLon = bound.Lng;
                    double semiMajorAxis = bound.Radius;  // Suuri akseli
                    double semiMinorAxis = bound.Radius / 2;  // Pieni akseli

                    var circleWaypoints = await GenerateWaypointsForCircleAsync(centerLat, centerLon, semiMajorAxis, semiMinorAxis, altitude, speed, action, id, photoInterval);
                    waypoints.AddRange(circleWaypoints);
                }
            }
            else if (boundsType == "polyline")
            {
                // For polylines, always use east-west pattern (ignore isNorthSouth parameter)
                var polylineWaypoints = GenerateWaypointsForShape(
                    bounds,
                    altitude,
                    speed,
                    angle,
                    in_distance,
                    photoInterval,
                    useEndpointsOnly,
                    ref id,
                    action,
                    false // Force isNorthSouth to false for polylines
                );
                waypoints.AddRange(polylineWaypoints);
            }

            // Ensure each waypoint points to the next one in sequence
            UpdateWaypointHeadings(waypoints);
            
            return await Task.FromResult(waypoints);
        }

        // Implementation for the new interface - delegates to the legacy method for now
        public async Task<List<Waypoint>> GenerateWaypointsAsync(List<ShapeData> shapes, WaypointParameters parameters)
        {
            _logger.LogWarning("New interface called on legacy WaypointService - this is not fully implemented");
            
            if (shapes == null || shapes.Count == 0 || shapes[0].Coordinates == null)
            {
                return new List<Waypoint>();
            }
            
            // Just use the first shape for compatibility
            var shape = shapes[0];
            string boundsType = shape.Type;
            List<Coordinate> bounds = shape.Coordinates;
            
            // For polylines, always force isNorthSouth to false
            bool effectiveIsNorthSouth = parameters.IsNorthSouth;
            if (boundsType == "polyline")
            {
                effectiveIsNorthSouth = false;
            }
            
            return await GenerateWaypointsAsync(
                parameters.Action,
                parameters.UnitType,
                parameters.Altitude,
                parameters.Speed,
                0, // Angle is no longer used
                parameters.LineSpacing,
                bounds,
                boundsType,
                parameters.StartingIndex,
                parameters.PhotoInterval,
                parameters.UseEndpointsOnly,
                effectiveIsNorthSouth // Use the potentially modified value
            );
        }

        private List<Waypoint> GenerateWaypointsForShape(
            List<Coordinate> coordinates,
            double altitude,
            double speed,
            int angle,
            double lineSpacing,
            double photoInterval,
            bool useEndpointsOnly,
            ref int id,
            string allPointsAction,
            bool isNorthSouth)  // This parameter is forced to false for all polylines
        {
            var waypoints = new List<Waypoint>();
            
            // Always force isNorthSouth to false for polylines regardless of input value
            isNorthSouth = false;
            
            // First determine if this is a closed polyline (first and last points are the same)
            bool isClosedPolyline = coordinates.Count > 2 && 
                      coordinates[0].Lat == coordinates[coordinates.Count - 1].Lat &&
                      coordinates[0].Lng == coordinates[coordinates.Count - 1].Lng;
            
            // Calculate distance between points based on photo interval
            double distancePerPhoto = speed * photoInterval;
            
            if (isClosedPolyline)
            {
                // For closed polylines (polygons), use a grid-based approach with the provided isNorthSouth parameter
                // Note: For polylines passed from GenerateWaypointsAsync, isNorthSouth will always be false
                // Determine shape bounds
                double minLat = coordinates.Min(c => c.Lat);
                double maxLat = coordinates.Max(c => c.Lat);
                double minLng = coordinates.Min(c => c.Lng);
                double maxLng = coordinates.Max(c => c.Lng);
                
                // Calculate line spacing and photo spacing
                double latStep = lineSpacing / 111320.0; // Convert lineSpacing from meters to degrees latitude
                double lngStep = lineSpacing / (111320.0 * Math.Cos(minLat * Math.PI / 180.0)); // Adjust for longitude
                
                if (isNorthSouth)
                {
                    // North-South direction
                    bool goingNorth = true; // To alternate direction
                    
                    for (double lng = minLng; lng <= maxLng; lng += lngStep)
                    {
                        List<double> intersectionLats = FindIntersectionLatitudes(coordinates, lng);
                        
                        // Sort and process pairs of intersections
                        if (intersectionLats.Count >= 2)
                        {
                            intersectionLats.Sort();
                            
                            // Process each pair of entry/exit points
                            for (int i = 0; i < intersectionLats.Count; i += 2)
                            {
                                if (i + 1 >= intersectionLats.Count) break;
                                
                                double startLat = intersectionLats[i];
                                double endLat = intersectionLats[i + 1];
                                
                                // For alternating direction
                                if (!goingNorth)
                                {
                                    double temp = startLat;
                                    startLat = endLat;
                                    endLat = temp;
                                }
                                
                                List<Waypoint> lineWaypoints = new List<Waypoint>();
                                
                                if (useEndpointsOnly)
                                {
                                    // Only add waypoints at the beginning and end of the line
                                    // Calculate heading based on the direction from start to end
                                    double pathHeading = CalculateHeading(startLat, lng, endLat, lng);
                                    
                                    lineWaypoints.Add(CreateWaypoint(
                                        startLat, lng, altitude, 
                                        pathHeading, // Use actual path heading instead of fixed N/S 
                                        angle, speed, ref id, allPointsAction));
                                    
                                    lineWaypoints.Add(CreateWaypoint(
                                        endLat, lng, altitude, 
                                        pathHeading, // Use actual path heading instead of fixed N/S
                                        angle, speed, ref id, allPointsAction));
                                }
                                else
                                {
                                    // Add waypoints along the entire line segment
                                    double distance = Math.Abs(endLat - startLat) * 111320.0; // Approximate distance in meters
                                    int numWaypoints = Math.Max(2, (int)(distance / distancePerPhoto) + 1);
                                    
                                    // Calculate heading based on the direction from start to end
                                    double pathHeading = CalculateHeading(startLat, lng, endLat, lng);
                                    
                                    for (int j = 0; j < numWaypoints; j++)
                                    {
                                        double fraction = (double)j / (numWaypoints - 1);
                                        double lat = startLat + fraction * (endLat - startLat);
                                        
                                        lineWaypoints.Add(CreateWaypoint(
                                            lat, lng, altitude, 
                                            pathHeading, // Use actual path heading
                                            angle, speed, ref id, allPointsAction));
                                    }
                                }
                                
                                waypoints.AddRange(lineWaypoints);
                            }
                        }
                        
                        goingNorth = !goingNorth; // Change direction for the next column
                    }
                }
                else
                {
                    // East-West direction
                    bool goingEast = true; // To alternate direction
                    
                    for (double lat = minLat; lat <= maxLat; lat += latStep)
                    {
                        List<double> intersectionLngs = FindIntersectionLongitudes(coordinates, lat);
                        
                        // Sort and process pairs of intersections
                        if (intersectionLngs.Count >= 2)
                        {
                            intersectionLngs.Sort();
                            
                            // Process each pair of entry/exit points
                            for (int i = 0; i < intersectionLngs.Count; i += 2)
                            {
                                if (i + 1 >= intersectionLngs.Count) break;
                                
                                double startLng = intersectionLngs[i];
                                double endLng = intersectionLngs[i + 1];
                                
                                // For alternating direction
                                if (!goingEast)
                                {
                                    double temp = startLng;
                                    startLng = endLng;
                                    endLng = temp;
                                }
                                
                                List<Waypoint> lineWaypoints = new List<Waypoint>();
                                
                                if (useEndpointsOnly)
                                {
                                    // Only add waypoints at the beginning and end of the line
                                    double pathHeading = CalculateHeading(lat, startLng, lat, endLng);
                                    
                                    lineWaypoints.Add(CreateWaypoint(
                                        lat, startLng, altitude, 
                                        pathHeading, // Use actual path heading instead of fixed E/W
                                        angle, speed, ref id, allPointsAction));
                                    
                                    lineWaypoints.Add(CreateWaypoint(
                                        lat, endLng, altitude, 
                                        pathHeading, // Use actual path heading instead of fixed E/W
                                        angle, speed, ref id, allPointsAction));
                                }
                                else
                                {
                                    // Add waypoints along the entire line segment
                                    double distance = Math.Abs(endLng - startLng) * 111320.0 * Math.Cos(lat * Math.PI / 180.0); // Approximate distance in meters
                                    int numWaypoints = Math.Max(2, (int)(distance / distancePerPhoto) + 1);
                                    
                                    // Calculate heading based on the direction from start to end
                                    double pathHeading = CalculateHeading(lat, startLng, lat, endLng);
                                    
                                    for (int j = 0; j < numWaypoints; j++)
                                    {
                                        double fraction = (double)j / (numWaypoints - 1);
                                        double lng = startLng + fraction * (endLng - startLng);
                                        
                                        lineWaypoints.Add(CreateWaypoint(
                                            lat, lng, altitude, 
                                            pathHeading, // Use actual path heading
                                            angle, speed, ref id, allPointsAction));
                                    }
                                }
                                
                                waypoints.AddRange(lineWaypoints);
                            }
                        }
                        
                        goingEast = !goingEast; // Change direction for the next row
                    }
                }
            }
            else
            {
                // For open polylines, follow the line segments directly
                for (int i = 0; i < coordinates.Count - 1; i++)
                {
                    // Calculate heading for this segment
                    double heading = CalculateHeading(
                        coordinates[i].Lat, coordinates[i].Lng,
                        coordinates[i + 1].Lat, coordinates[i + 1].Lng);
                    
                    // Always add the start point of the segment (except for subsequent segments, to avoid duplicates)
                    if (i == 0 || !useEndpointsOnly)
                    {
                        waypoints.Add(CreateWaypoint(
                            coordinates[i].Lat, coordinates[i].Lng, 
                            altitude, heading, angle, speed, 
                            ref id, allPointsAction));
                    }
                    
                    // For the segment between vertices
                    double segmentDistance = CalculateDistance(
                        coordinates[i].Lat, coordinates[i].Lng,
                        coordinates[i + 1].Lat, coordinates[i + 1].Lng);
                    
                    // Skip intermediate points if using endpoints only
                    if (!useEndpointsOnly)
                    {
                        // Calculate how many intermediate points to add
                        int numIntermediatePoints = Math.Max(0, (int)(segmentDistance / distancePerPhoto) - 1);
                        
                        for (int j = 1; j <= numIntermediatePoints; j++)
                        {
                            double fraction = j / (double)(numIntermediatePoints + 1);
                            
                            // Interpolate position
                            double lat = coordinates[i].Lat + fraction * (coordinates[i + 1].Lat - coordinates[i].Lat);
                            double lng = coordinates[i].Lng + fraction * (coordinates[i + 1].Lng - coordinates[i].Lng);
                            
                            waypoints.Add(CreateWaypoint(
                                lat, lng, altitude, heading, 
                                angle, speed, ref id, allPointsAction));
                        }
                    }
                }
                
                // Always add the last vertex
                if (coordinates.Count > 1)
                {
                    var lastCoord = coordinates[coordinates.Count - 1];
                    var secondLastCoord = coordinates[coordinates.Count - 2];
                    
                    double finalHeading = CalculateHeading(
                        secondLastCoord.Lat, secondLastCoord.Lng,
                        lastCoord.Lat, lastCoord.Lng);
                    
                    waypoints.Add(CreateWaypoint(
                        lastCoord.Lat, lastCoord.Lng, 
                        altitude, finalHeading, angle, speed, 
                        ref id, allPointsAction));
                }
            }
            
            // Ensure each waypoint points to the next one in sequence
            UpdateWaypointHeadings(waypoints);
            
            return waypoints;
        }
        
        // Helper method to calculate heading from point 1 to point 2 in degrees
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
        
        // Helper method to calculate the distance between two points in meters
        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double EarthRadiusMeters = 6371000;
            
            // Convert to radians
            double lat1Rad = lat1 * Math.PI / 180.0;
            double lng1Rad = lng1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            double lng2Rad = lng2 * Math.PI / 180.0;
            
            // Haversine formula
            double dLat = lat2Rad - lat1Rad;
            double dLng = lng2Rad - lng1Rad;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return EarthRadiusMeters * c;
        }
        
        // Helper method to find the latitudes where a vertical line at the given longitude intersects with the polygon
        private List<double> FindIntersectionLatitudes(List<Coordinate> polygon, double lng)
        {
            List<double> intersections = new List<double>();
            
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i].Lng <= lng && lng < polygon[j].Lng) || 
                    (polygon[j].Lng <= lng && lng < polygon[i].Lng))
                {
                    // Line intersects this edge, calculate the latitude of intersection
                    if (Math.Abs(polygon[i].Lng - polygon[j].Lng) > 1e-10) // Avoid division by zero
                    {
                        double lat = polygon[i].Lat + (polygon[j].Lat - polygon[i].Lat) * 
                            (lng - polygon[i].Lng) / (polygon[j].Lng - polygon[i].Lng);
                        
                        // Add to list if not a duplicate
                        bool isDuplicate = false;
                        foreach (var existing in intersections)
                        {
                            if (Math.Abs(existing - lat) < 1e-10)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                        
                        if (!isDuplicate)
                        {
                            intersections.Add(lat);
                        }
                    }
                    else if (Math.Abs(lng - polygon[i].Lng) < 1e-10)
                    {
                        // The line coincides with a vertical edge
                        // Add both endpoints
                        intersections.Add(polygon[i].Lat);
                        intersections.Add(polygon[j].Lat);
                    }
                }
            }
            
            return intersections;
        }
        
        // Helper method to find the longitudes where a horizontal line at the given latitude intersects with the polygon
        private List<double> FindIntersectionLongitudes(List<Coordinate> polygon, double lat)
        {
            List<double> intersections = new List<double>();
            
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i].Lat <= lat && lat < polygon[j].Lat) || 
                    (polygon[j].Lat <= lat && lat < polygon[i].Lat))
                {
                    // Line intersects this edge, calculate the longitude of intersection
                    if (Math.Abs(polygon[i].Lat - polygon[j].Lat) > 1e-10) // Avoid division by zero
                    {
                        double lng = polygon[i].Lng + (polygon[j].Lng - polygon[i].Lng) * 
                            (lat - polygon[i].Lat) / (polygon[j].Lat - polygon[i].Lat);
                        
                        // Add to list if not a duplicate
                        bool isDuplicate = false;
                        foreach (var existing in intersections)
                        {
                            if (Math.Abs(existing - lng) < 1e-10)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                        
                        if (!isDuplicate)
                        {
                            intersections.Add(lng);
                        }
                    }
                    else if (Math.Abs(lat - polygon[i].Lat) < 1e-10)
                    {
                        // The line coincides with a horizontal edge
                        // Add both endpoints
                        intersections.Add(polygon[i].Lng);
                        intersections.Add(polygon[j].Lng);
                    }
                }
            }
            
            return intersections;
        }

        public Waypoint CreateWaypoint(double lat, double lng, double altitude, double heading, int angle, double speed, ref int id, string action)
        {
            return new Waypoint
            {
                Lat = lat,
                Lng = lng,
                Alt = altitude,
                Speed = speed,
                Index = id++,
                Action = action,
                Heading = heading // Ensure heading is explicitly set
            };
        }

        public bool IsPointInPolygon(List<Coordinate> polygon, double lat, double lng)
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

        private int Orientation(Coordinate p, Coordinate q, Coordinate r)
        {
            double val = (q.Lng - p.Lng) * (r.Lat - q.Lat) - (q.Lat - p.Lat) * (r.Lng - q.Lng);
            if (val == 0) return 0;  // Collinear
            return (val > 0) ? 1 : 2; // Clockwise or counterclockwise
        }

        private bool OnSegment(Coordinate p, Coordinate q, Coordinate r)
        {
            return q.Lng <= Math.Max(p.Lng, r.Lng) && q.Lng >= Math.Min(p.Lng, r.Lng) &&
                   q.Lat <= Math.Max(p.Lat, r.Lat) && q.Lat >= Math.Min(p.Lat, r.Lat);
        }

        // Suorakulmion tai polygonin reittipisteiden generointilogiikka
        private List<Waypoint> GenerateWaypointsForRectangleOrPolygon(
            List<Coordinate> coordinates,
            double altitude,
            double speed,
            int angle,
            double in_distance,
            bool useEndpointsOnly,
            bool isNorthSouth,
            ref int id,
            string allPointsAction)
        {
            var waypoints = new List<Waypoint>();

            // Määritetään alueen rajat (min ja max lat/lng)
            double minLat = coordinates.Min(c => c.Lat);
            double maxLat = coordinates.Max(c => c.Lat);
            double minLng = coordinates.Min(c => c.Lng);
            double maxLng = coordinates.Max(c => c.Lng);

            if (isNorthSouth)
            {
                // Pohjoinen-Etelä suunnassa
                double lngDistanceInDegrees = in_distance / (111320.0 * Math.Cos(minLat * Math.PI / 180.0));  // Muunnetaan longitude metriarvoksi
                for (double lng = minLng; lng <= maxLng; lng += lngDistanceInDegrees)
                {
                    // Ensimmäinen reittipiste
                    waypoints.Add(new Waypoint
                    {
                        Lat = minLat,
                        Lng = lng,
                        Alt = altitude,
                        Speed = speed,
                        Index = id++,
                        Action = allPointsAction
                    });

                    // Toinen reittipiste
                    waypoints.Add(new Waypoint
                    {
                        Lat = maxLat,
                        Lng = lng,
                        Alt = altitude,
                        Speed = speed,
                        Index = id++,
                        Action = allPointsAction
                    });

                    if (useEndpointsOnly)
                    {
                        // Päivitetään longitude seuraavalle arvolle ja lisätään vain yksi piste
                        lng += lngDistanceInDegrees;
                        if (lng <= maxLng)
                        {
                            waypoints.Add(new Waypoint
                            {
                                Lat = maxLat,
                                Lng = lng,
                                Alt = altitude,
                                Speed = speed,
                                Index = id++,
                                Action = allPointsAction
                            });

                            waypoints.Add(new Waypoint
                            {
                                Lat = minLat,
                                Lng = lng,
                                Alt = altitude,
                                Speed = speed,
                                Index = id++,
                                Action = allPointsAction
                            });
                        }
                    }
                }
            }
            else
            {
                // Itä-Länsi suunnassa
                double latDistanceInDegrees = in_distance / 111320.0;  // Muunnetaan latitude metriarvoksi
                for (double lat = minLat; lat <= maxLat; lat += latDistanceInDegrees)
                {
                    // Ensimmäinen reittipiste
                    waypoints.Add(new Waypoint
                    {
                        Lat = lat,
                        Lng = minLng,
                        Alt = altitude,
                        Speed = speed,
                        Index = id++,
                        Action = allPointsAction
                    });

                    // Toinen reittipiste
                    waypoints.Add(new Waypoint
                    {
                        Lat = lat,
                        Lng = maxLng,
                        Alt = altitude,
                        Speed = speed,
                        Index = id++,
                        Action = allPointsAction
                    });

                    if (useEndpointsOnly)
                    {
                        // Päivitetään latitude seuraavalle arvolle ja lisätään vain yksi piste
                        lat += latDistanceInDegrees;
                        if (lat <= maxLat)
                        {
                            waypoints.Add(new Waypoint
                            {
                                Lat = lat,
                                Lng = maxLng,
                                Alt = altitude,
                                Speed = speed,
                                Index = id++,
                                Action = allPointsAction
                            });

                            waypoints.Add(new Waypoint
                            {
                                Lat = lat,
                                Lng = minLng,
                                Alt = altitude,
                                Speed = speed,
                                Index = id++,
                                Action = allPointsAction
                            });
                        }
                    }
                }
            }

            // Update all waypoint headings to ensure each points to the next one
            UpdateWaypointHeadings(waypoints);
            
            return waypoints;
        }

        // Ympyrän reittipisteiden generointilogiikka
        public async Task<List<Waypoint>> GenerateWaypointsForCircleAsync(
            double centerLat,
            double centerLon,
            double semiMajorAxis,
            double semiMinorAxis,
            double altitude,
            double speed,
            string allPointsAction,
            int startingId,
            double photoInterval)
        {
            var waypoints = new List<Waypoint>();
            double circumference = 2 * Math.PI * Math.Sqrt((semiMajorAxis * semiMajorAxis + semiMinorAxis * semiMinorAxis) / 2);  // Ellipsin arvioitu kehä
            double distancePerPhoto = speed * photoInterval;
            int numberOfWaypoints = (int)(circumference / distancePerPhoto);

            double angleStep = 360.0 / numberOfWaypoints;  // Jaetaan ellipsi yhtä suuriin kulmiin
            double distanceCovered = 0;
            int id = startingId;

            // Generate points around the ellipse
            List<Waypoint> tempWaypoints = new List<Waypoint>();
            
            for (int i = 0; i < numberOfWaypoints; i++)
            {
                double angle = i * angleStep;  // Laske kulma nykyiselle reittipisteelle

                // Muunna kulma radiaaneiksi
                double angleRad = angle * (Math.PI / 180.0);

                // Parametriset yhtälöt ellipsin reittipisteiden laskemiseen
                double waypointLat = centerLat + (semiMajorAxis / 111000.0) * Math.Cos(angleRad);  // Latitude displacement suurta akselia pitkin
                double waypointLon = centerLon + (semiMinorAxis / (111000.0 * Math.Cos(centerLat * Math.PI / 180.0))) * Math.Sin(angleRad);  // Longitude displacement pientä akselia pitkin

                if (distanceCovered >= distancePerPhoto || i == 0)
                {
                    // Luo ja lisää reittipiste
                    tempWaypoints.Add(new Waypoint
                    {
                        Lat = waypointLat,
                        Lng = waypointLon,
                        Alt = altitude,
                        Speed = speed,
                        Index = id++,
                        Action = allPointsAction
                    });

                    distanceCovered = 0;
                }

                distanceCovered += circumference / numberOfWaypoints;  // Päivitä matkattu etäisyys
            }
            
            // Make sure we have at least 2 waypoints to form a complete circle
            if (tempWaypoints.Count < 2)
            {
                // Add at least one more point to make a circle
                double angleRad = 180.0 * (Math.PI / 180.0); // Opposite side of circle
                double waypointLat = centerLat + (semiMajorAxis / 111000.0) * Math.Cos(angleRad);
                double waypointLon = centerLon + (semiMinorAxis / (111000.0 * Math.Cos(centerLat * Math.PI / 180.0))) * Math.Sin(angleRad);
                
                tempWaypoints.Add(new Waypoint
                {
                    Lat = waypointLat,
                    Lng = waypointLon,
                    Alt = altitude,
                    Speed = speed,
                    Index = id++,
                    Action = allPointsAction
                });
            }
            
            // For a complete circle, add the first point again at the end to close the loop
            if (tempWaypoints.Count > 0)
            {
                tempWaypoints.Add(new Waypoint
                {
                    Lat = tempWaypoints[0].Lat,
                    Lng = tempWaypoints[0].Lng,
                    Alt = altitude,
                    Speed = speed,
                    Index = id++,
                    Action = allPointsAction
                });
            }
            
            // Update all waypoint headings to ensure each points to the next one
            UpdateWaypointHeadings(tempWaypoints);
            
            return await Task.FromResult(tempWaypoints);  // Palauta tulos Task-objektina
        }

        // Helper method to update all waypoint headings to point to the next waypoint
        private void UpdateWaypointHeadings(List<Waypoint> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2)
                return;
                
            // Update each waypoint to point to the next one
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Waypoint current = waypoints[i];
                Waypoint next = waypoints[i + 1];
                
                double heading = CalculateHeading(current.Lat, current.Lng, next.Lat, next.Lng);
                current.Heading = heading;
            }
            
            // For the last waypoint, keep its existing heading (from its segment)
            // or optionally make it point back to the first waypoint for a closed loop
            // waypoints[waypoints.Count - 1].Heading = CalculateHeading(
            //     waypoints[waypoints.Count - 1].Lat, waypoints[waypoints.Count - 1].Lng,
            //     waypoints[0].Lat, waypoints[0].Lng);
        }
    }
}
