using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using SharpKml.Dom;
using Waypoint = KarttaBackEnd2.Server.Models.Waypoint;

namespace KarttaBackEnd2.Server.Services
{
    public class WaypointService : IWaypointService
    {
        // Generoi reittipisteitä polyline-muodolle
        public async Task<List<Waypoint>> GenerateWaypointsAsync(
            string allPointsAction,
            int unitType_in,
            double altitude,
            double speed,  // Nopeus metreinä sekunnissa
            int angle,
            double in_distance,
            List<Coordinate> bounds,  // Koordinaatit
            string boundsType,  // Muodon tyyppi, kuten "polyline", "rectangle", "circle"
            int in_startingIndex,
            double photoInterval = 3,
            bool useEndpointsOnly = false,  // Kontrolloi päätepisteiden logiikkaa
            bool isNorthSouth = false  // Uusi parametri suunnan valintaan: true = Pohjoinen-Etelä, false = Itä-Länsi
        )
        {
            var waypoints = new List<Waypoint>();
            int id = in_startingIndex;
            allPointsAction = "takePhoto";

            if (boundsType == "rectangle" || boundsType == "polygon")
            {
                // Alkuperäinen suorakulmion/polygonin käsittelylogiikka
                waypoints.AddRange(GenerateWaypointsForRectangleOrPolygon(bounds, altitude, speed, angle, in_distance, useEndpointsOnly, isNorthSouth, ref id, allPointsAction));
            }
            else if (boundsType == "circle")
            {
                // Ympyrän reittien generointilogiikka
                foreach (var bound in bounds)
                {
                    double centerLat = bound.Lat;
                    double centerLon = bound.Lng;
                    double semiMajorAxis = bound.Radius;  // Suuri akseli
                    double semiMinorAxis = bound.Radius / 2;  // Pieni akseli

                    // Luo ympyrän tai ellipsin reittipisteet
                    var circleWaypoints = await GenerateWaypointsForCircleAsync(centerLat, centerLon, semiMajorAxis, semiMinorAxis, altitude, speed, allPointsAction, id, photoInterval);
                    waypoints.AddRange(circleWaypoints);
                }
            }
            else if (boundsType == "polyline")
            {
                // Polyline-logiikka: luodaan reittipisteitä polyline-muodolle
                var polylineWaypoints =  // Kutsu GenerateWaypointsForShape metodia
                 waypoints = GenerateWaypointsForShape(
                    bounds,
                    altitude,
                    speed,
                    angle,
                    in_distance,
                    photoInterval,
                    useEndpointsOnly,
                    
                    ref id,
                    allPointsAction
        );
                waypoints.AddRange(polylineWaypoints);
            }

            return await Task.FromResult(waypoints);
        }

        // Polyline-reittipisteiden generointi
        private List<Waypoint> GenerateWaypointsForPolyline(
            List<Coordinate> coordinates,
            double in_distance,
            double photoInterval,
            bool useEndpointsOnly,
            bool isNorthSouth,
            double altitude,
            double speed,
            int angle,
            ref int id,
            string allPointsAction)
        {
            var waypoints = new List<Waypoint>();

            // Määritä ulkoreunat Convex Hull -algoritmilla
            var hull = GetConvexHull(coordinates);

            // Nyt käsitellään samalla tavalla kuin suorakulmion reittipisteiden generointi
            double minLat = hull.Min(b => b.Lat);
            double maxLat = hull.Max(b => b.Lat);
            double minLng = hull.Min(b => b.Lng);
            double maxLng = hull.Max(b => b.Lng);

            if (isNorthSouth)
            {
                // Pohjoinen-Etelä suunnassa
                double lngDistanceInDegrees = in_distance / (111320.0 * Math.Cos(minLat * Math.PI / 180.0));
                for (double lng = minLng; lng <= maxLng; lng += lngDistanceInDegrees)
                {
                    waypoints.Add(new Waypoint
                    {
                        Latitude = minLat,
                        Longitude = lng,
                        Altitude = altitude,
                        Heading = 0,  // Pohjoinen
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
                        Action = allPointsAction
                    });

                    waypoints.Add(new Waypoint
                    {
                        Latitude = maxLat,
                        Longitude = lng,
                        Altitude = altitude,
                        Heading = 180,  // Etelä
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
                        Action = allPointsAction
                    });

                    if (useEndpointsOnly)
                    {
                        lng += lngDistanceInDegrees;
                        if (lng <= maxLng)
                        {
                            waypoints.Add(new Waypoint
                            {
                                Latitude = maxLat,
                                Longitude = lng,
                                Altitude = altitude,
                                Heading = 0,
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            waypoints.Add(new Waypoint
                            {
                                Latitude = minLat,
                                Longitude = lng,
                                Altitude = altitude,
                                Heading = 180,
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });
                        }
                    }
                }
            }
            else
            {
                // Itä-Länsi suunnassa
                double latDistanceInDegrees = in_distance / 111320.0;
                for (double lat = minLat; lat <= maxLat; lat += latDistanceInDegrees)
                {
                    waypoints.Add(new Waypoint
                    {
                        Latitude = lat,
                        Longitude = minLng,
                        Altitude = altitude,
                        Heading = 90,  // Itä
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
                        Action = allPointsAction
                    });

                    waypoints.Add(new Waypoint
                    {
                        Latitude = lat,
                        Longitude = maxLng,
                        Altitude = altitude,
                        Heading = 270,  // Länsi
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
                        Action = allPointsAction
                    });

                    if (useEndpointsOnly)
                    {
                        lat += latDistanceInDegrees;
                        if (lat <= maxLat)
                        {
                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,
                                Longitude = maxLng,
                                Altitude = altitude,
                                Heading = 270,
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,
                                Longitude = minLng,
                                Altitude = altitude,
                                Heading = 90,
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });
                        }
                    }
                }
            }

            return waypoints;
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
      string allPointsAction)
        {
            var waypoints = new List<Waypoint>();

            // Determine shape bounds
            double minLat = coordinates.Min(c => c.Lat);
            double maxLat = coordinates.Max(c => c.Lat);
            double minLng = coordinates.Min(c => c.Lng);
            double maxLng = coordinates.Max(c => c.Lng);

            // Calculate steps
            double latStep = in_distance / 111320.0; // Convert in_distance from meters to degrees latitude
            double lngStep = photoInterval * speed / (111320.0 * Math.Cos(minLat * Math.PI / 180.0)); // Distance covered in one photo interval

            bool goingEast = true; // To alternate direction

            for (double lat = minLat; lat <= maxLat; lat += latStep)
            {
                List<Waypoint> rowWaypoints = new List<Waypoint>();

                if (goingEast)
                {
                    for (double lng = minLng; lng <= maxLng; lng += lngStep)
                    {
                        if (IsPointInPolygon(coordinates, lat, lng))
                        {
                            rowWaypoints.Add(CreateWaypoint(lat, lng, altitude, 90, angle, speed, ref id, allPointsAction));
                        }
                    }
                }
                else
                {
                    for (double lng = maxLng; lng >= minLng; lng -= lngStep)
                    {
                        if (IsPointInPolygon(coordinates, lat, lng))
                        {
                            rowWaypoints.Add(CreateWaypoint(lat, lng, altitude, 270, angle, speed, ref id, allPointsAction));
                        }
                    }
                }

                if (useEndpointsOnly && rowWaypoints.Count > 0)
                {
                    waypoints.Add(rowWaypoints.First());
                    if (rowWaypoints.Count > 1)
                    {
                        waypoints.Add(rowWaypoints.Last());
                    }
                }
                else
                {
                    waypoints.AddRange(rowWaypoints);
                }

                goingEast = !goingEast; // Change direction for the next row
            }

            return waypoints;
        }


        private Waypoint CreateWaypoint(double lat, double lng, double altitude, double heading, int angle, double speed, ref int id, string action)
        {
            return new Waypoint
            {
                Latitude = lat,
                Longitude = lng,
                Altitude = altitude,
                Heading = heading,
                GimbalAngle = angle,
                Speed = speed,
                Id = id++,
                Action = action
            };
        }

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

        private bool IsEdgePoint(List<Coordinate> polygon, double lat, double lng, double tolerance)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                Coordinate p1 = polygon[i];
                Coordinate p2 = polygon[(i + 1) % polygon.Count];

                // Tarkistetaan, onko piste lähellä jotain reunaviivaa
                if (DistanceToSegment(lat, lng, p1, p2) <= tolerance)
                {
                    return true;
                }
            }
            return false;
        }


        private double DistanceToSegment(double lat, double lng, Coordinate p1, Coordinate p2)
        {
            // Lasketaan etäisyys pisteestä (lat, lng) segmenttiin (p1, p2)
            double x0 = lng, y0 = lat;
            double x1 = p1.Lng, y1 = p1.Lat;
            double x2 = p2.Lng, y2 = p2.Lat;

            double dx = x2 - x1;
            double dy = y2 - y1;

            if (dx == 0 && dy == 0)
            {
                // p1 ja p2 ovat samat pisteet
                return Math.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));
            }

            // Parametrinen arvo t linjan pituudella
            double t = ((x0 - x1) * dx + (y0 - y1) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            // Projektio pisteeseen linjalla
            double projX = x1 + t * dx;
            double projY = y1 + t * dy;

            // Palautetaan etäisyys projektioon
            return Math.Sqrt((x0 - projX) * (x0 - projX) + (y0 - projY) * (y0 - projY));
        }

        private int CountIntersections(List<Coordinate> polygon, Coordinate start, Coordinate end)
        {
            int intersections = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Coordinate p1 = polygon[i];
                Coordinate p2 = polygon[(i + 1) % polygon.Count];

                if (DoIntersect(start, end, p1, p2))
                {
                    intersections++;
                }
            }
            return intersections;
        }

        private bool DoIntersect(Coordinate p1, Coordinate q1, Coordinate p2, Coordinate q2)
        {
            // Check if line segments (p1,q1) and (p2,q2) intersect
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            if (o1 != o2 && o3 != o4)
                return true;

            // Special cases for collinear points
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

            return false;
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

        private double DetermineHeading(List<Coordinate> polygon, double lat, double lng)
        {
            Coordinate current = new Coordinate { Lat = lat, Lng = lng };
            Coordinate nearestEdgePoint = FindNearestEdgePoint(polygon, current);
            return CalculateBearing(current, nearestEdgePoint);
        }

        private Coordinate FindNearestEdgePoint(List<Coordinate> polygon, Coordinate point)
        {
            Coordinate nearestPoint = polygon[0];
            double minDistance = double.MaxValue;

            for (int i = 0; i < polygon.Count; i++)
            {
                Coordinate p1 = polygon[i];
                Coordinate p2 = polygon[(i + 1) % polygon.Count];

                Coordinate projection = ProjectPointOnLine(point, p1, p2);
                double distance = CalculateDistance(point, projection);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = projection;
                }
            }

            return nearestPoint;
        }

        private Coordinate ProjectPointOnLine(Coordinate p, Coordinate a, Coordinate b)
        {
            double ax = b.Lng - a.Lng;
            double ay = b.Lat - a.Lat;
            double t = ((p.Lng - a.Lng) * ax + (p.Lat - a.Lat) * ay) / (ax * ax + ay * ay);
            t = Math.Max(0, Math.Min(1, t));

            return new Coordinate
            {
                Lng = a.Lng + t * ax,
                Lat = a.Lat + t * ay
            };
        }

        private double CalculateDistance(Coordinate p1, Coordinate p2)
        {
            const double R = 6371000; // Earth radius in meters
            double lat1 = p1.Lat * Math.PI / 180;
            double lat2 = p2.Lat * Math.PI / 180;
            double deltaLat = (p2.Lat - p1.Lat) * Math.PI / 180;
            double deltaLng = (p2.Lng - p1.Lng) * Math.PI / 180;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double CalculateBearing(Coordinate start, Coordinate end)
        {
            double startLat = start.Lat * Math.PI / 180;
            double startLng = start.Lng * Math.PI / 180;
            double endLat = end.Lat * Math.PI / 180;
            double endLng = end.Lng * Math.PI / 180;

            double y = Math.Sin(endLng - startLng) * Math.Cos(endLat);
            double x = Math.Cos(startLat) * Math.Sin(endLat) -
                       Math.Sin(startLat) * Math.Cos(endLat) * Math.Cos(endLng - startLng);
            double bearing = Math.Atan2(y, x);

            return (bearing * 180 / Math.PI + 360) % 360; // Convert to degrees
        }

        private Coordinate CalculatePointByDistance(double lat, double lng, double distance, double bearing)
        {
            const double R = 6371000; // Earth radius in meters
            double bearingRad = bearing * Math.PI / 180;
            double latRad = lat * Math.PI / 180;
            double lngRad = lng * Math.PI / 180;

            double newLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(distance / R) +
                                         Math.Cos(latRad) * Math.Sin(distance / R) * Math.Cos(bearingRad));
            double newLngRad = lngRad + Math.Atan2(Math.Sin(bearingRad) * Math.Sin(distance / R) * Math.Cos(latRad),
                                                   Math.Cos(distance / R) - Math.Sin(latRad) * Math.Sin(newLatRad));

            return new Coordinate
            {
                Lat = newLatRad * 180 / Math.PI,
                Lng = newLngRad * 180 / Math.PI
            };
        }
        /*
        private bool IsPointInPolygon(List<Coordinate> polygon, double lat, double lng)
        {
            // Algoritmi tarkistaa, onko annettu piste polygonin sisällä
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i].Lng > lng) != (polygon[j].Lng > lng) &&
                    lat < (polygon[j].Lat - polygon[i].Lat) * (lng - polygon[i].Lng) / (polygon[j].Lng - polygon[i].Lng) + polygon[i].Lat)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        */


        // Alueen ulkoreunojen määrittäminen Convex Hull -algoritmilla
        private List<Coordinate> GetConvexHull(List<Coordinate> points)
        {
            points = points.OrderBy(p => p.Lng).ThenBy(p => p.Lat).ToList();

            if (points.Count <= 1) return points;

            List<Coordinate> lower = new List<Coordinate>();
            foreach (var p in points)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                {
                    lower.RemoveAt(lower.Count - 1);
                }
                lower.Add(p);
            }

            List<Coordinate> upper = new List<Coordinate>();
            for (int i = points.Count - 1; i >= 0; i--)
            {
                var p = points[i];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                {
                    upper.RemoveAt(upper.Count - 1);
                }
                upper.Add(p);
            }

            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);

            lower.AddRange(upper);
            return lower;
        }

        private double Cross(Coordinate o, Coordinate a, Coordinate b)
        {
            return (a.Lng - o.Lng) * (b.Lat - o.Lat) - (a.Lat - o.Lat) * (b.Lng - o.Lng);
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
                        Latitude = minLat,
                        Longitude = lng,
                        Altitude = altitude,
                        Heading = 0,  // Pohjoinen
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
                        Action = allPointsAction
                    });

                    // Toinen reittipiste
                    waypoints.Add(new Waypoint
                    {
                        Latitude = maxLat,
                        Longitude = lng,
                        Altitude = altitude,
                        Heading = 180,  // Etelä
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
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
                                Latitude = maxLat,
                                Longitude = lng,
                                Altitude = altitude,
                                Heading = 180,  // Etelä
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            waypoints.Add(new Waypoint
                            {
                                Latitude = minLat,
                                Longitude = lng,
                                Altitude = altitude,
                                Heading = 0,  // Pohjoinen
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
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
                        Latitude = lat,
                        Longitude = minLng,
                        Altitude = altitude,
                        Heading = 90,  // Itä
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
                        Action = allPointsAction
                    });

                    // Toinen reittipiste
                    waypoints.Add(new Waypoint
                    {
                        Latitude = lat,
                        Longitude = maxLng,
                        Altitude = altitude,
                        Heading = 270,  // Länsi
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,
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
                                Latitude = lat,
                                Longitude = maxLng,
                                Altitude = altitude,
                                Heading = 270,  // Länsi
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,
                                Longitude = minLng,
                                Altitude = altitude,
                                Heading = 90,  // Itä
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });
                        }
                    }
                }
            }

            return waypoints;
        }


        // Ympyrän reittipisteiden generointilogiikka
        private async Task<List<Waypoint>> GenerateWaypointsForCircleAsync(
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
                    // Laske suunta ellipsin keskipisteeseen
                    double deltaLat = centerLat - waypointLat;
                    double deltaLon = centerLon - waypointLon;
                    double heading = Math.Atan2(deltaLon, deltaLat) * (180.0 / Math.PI);  // Muunna radiaaneista asteiksi

                    // Luo ja lisää reittipiste
                    waypoints.Add(new Waypoint
                    {
                        Latitude = waypointLat,
                        Longitude = waypointLon,
                        Altitude = altitude,
                        Heading = heading,  // Suunta kohti ellipsin keskipistettä
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,  // Lisää yksilöivä Id jokaiselle reittipisteelle
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
