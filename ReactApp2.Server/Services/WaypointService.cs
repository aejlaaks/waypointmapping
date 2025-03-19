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
                waypoints = GenerateWaypointsForShape(
                    bounds,
                    altitude,
                    speed,
                    angle,
                    in_distance,
                    photoInterval,
                    useEndpointsOnly,
                    ref id,
                    action,
                    isNorthSouth
                );
            }

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
                parameters.IsNorthSouth
            );
        }

        private List<Waypoint> GenerateWaypointsForShape(
            List<Coordinate> coordinates,
            double altitude,
            double speed,
            int angle,
            double in_distance,
            double photoInterval,
            bool useEndpointsOnly,
            ref int id,
            string allPointsAction,
            bool isNorthSouth)
        {
            var waypoints = new List<Waypoint>();

            // For polylines, we'll generate waypoints directly on the line segments
            if (coordinates.Count < 2)
            {
                return waypoints;
            }

            // Simple case: just add waypoints at each coordinate
            if (useEndpointsOnly)
            {
                foreach (var coordinate in coordinates)
                {
                    waypoints.Add(CreateWaypoint(coordinate.Lat, coordinate.Lng, altitude, 0, 0, speed, ref id, allPointsAction));
                }
                return waypoints;
            }

            // Create waypoints along each segment based on distance
            for (int i = 0; i < coordinates.Count - 1; i++)
            {
                var start = coordinates[i];
                var end = coordinates[i + 1];
                
                // Calculate heading
                double heading = Math.Atan2(end.Lng - start.Lng, end.Lat - start.Lat) * 180.0 / Math.PI;
                if (heading < 0) heading += 360.0;
                
                // Calculate segment distance in meters
                double segmentDistance = CalculateDistance(start.Lat, start.Lng, end.Lat, end.Lng);
                
                // Calculate number of points to add
                int numPoints = Math.Max(2, (int)(segmentDistance / (speed * photoInterval)));
                
                // Add waypoints along the segment
                for (int j = 0; j < numPoints; j++)
                {
                    double t = j / (double)(numPoints - 1);
                    double lat = start.Lat + t * (end.Lat - start.Lat);
                    double lng = start.Lng + t * (end.Lng - start.Lng);
                    
                    waypoints.Add(CreateWaypoint(lat, lng, altitude, heading, 0, speed, ref id, allPointsAction));
                }
            }

            return waypoints;
        }

        // Helper method to calculate distance between coordinates
        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double earthRadius = 6371000; // meters
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLng = (lng2 - lng1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
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
                Action = action
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

            for (int i = 0; i < numberOfWaypoints; i++)
            {
                double angle = i * angleStep;  // Laske kulma nykyiselle reittipisteelle

                // Muunna kulma radiaaneiksi
                double angleRad = angle * (Math.PI / 180.0);

                // Parametriset yhtälöt ellipsin reittipisteiden laskemiseen
                double waypointLat = centerLat + (semiMajorAxis / 111000.0) * Math.Cos(angleRad);  // Latitude displacement suurta akselia pitkin
                double waypointLon = centerLon + (semiMinorAxis / (111000.0 * Math.Cos(centerLat * Math.PI / 180.0))) * Math.Sin(angleRad);  // Longitude displacement pientä akselia pitkin

                if (distanceCovered >= distancePerPhoto)
                {
                    // Luo ja lisää reittipiste
                    waypoints.Add(new Waypoint
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

            return await Task.FromResult(waypoints);  // Palauta tulos Task-objektina
        }
    }
}
