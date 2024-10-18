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


        public Waypoint CreateWaypoint(double lat, double lng, double altitude, double heading, int angle, double speed, ref int id, string action)
        {
            return new Waypoint
            {
                Latitude = lat,
                Longitude = lng,
                Altitude = altitude,
                Heading = heading,
                Yaw = heading,
                GimbalAngle = angle,
                Speed = speed,
                Id = id++,
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
