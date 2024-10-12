using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using SharpKml.Dom;
using Waypoint = KarttaBackEnd2.Server.Models.Waypoint;

namespace KarttaBackEnd2.Server.Services
{
    public class WaypointService : IWaypointService
    {
        // Method to generate waypoints
        public async Task<List<Waypoint>> GenerateWaypointsAsync(string allPointsAction ,
            int unitType_in,
            double altitude,
            double speed,  // Speed in meters/second
            int angle,
            double in_distance,
            List<Coordinate> bounds,  // Rectangle coordinates come from 'bounds'
            string boundsType,
            int in_startingIndex,
            double photoInterval = 3,
            bool useEndpointsOnly = false  // New parameter to control endpoint-only logic
)  // Photo interval in seconds
        {
            var waypoints = new List<Waypoint>();
            int id = in_startingIndex;
            allPointsAction = "takePhoto";

            if (boundsType == "rectangle")
            {
                // Määritetään suorakulmion rajat
                double minLat = bounds.Min(b => b.Lat);  // Min latitude (etelä)
                double maxLat = bounds.Max(b => b.Lat);  // Max latitude (pohjoinen)
                double minLng = bounds.Min(b => b.Lng);  // Min longitude (länsi)
                double maxLng = bounds.Max(b => b.Lng);  // Max longitude (itä)

                // Muunnetaan in_distance metreinä asteiksi (latitude-arvot)
                double latDistanceInDegrees = in_distance / 111320.0;  // 1 leveysaste = 111320 metriä
                double currentAltitude = altitude;  // Korkeus pysyy vakiona
                bool goingEast = true;  // Pitää yllä suunnan (itään tai länteen)

                // Jos käytetään useEndpointsOnly-logiikkaa, laitetaan se omaan silmukkaan
                if (useEndpointsOnly)
                {
                    for (double lat = minLat; lat <= maxLat; lat += latDistanceInDegrees)
                    {
                        // 1. Lisätään lähtöpiste (vaakasuuntainen piste) koordinaattien mukaan
                        waypoints.Add(new Waypoint
                        {
                            Latitude = lat,
                            Longitude = minLng,  // Länsi (min longitude)
                            Altitude = currentAltitude,  // Sama altitude
                            Heading = 90,  // Itä
                            GimbalAngle = angle,
                            Speed = speed,
                            Id = id++,
                            Action = allPointsAction  // Oletusarvoisesti otetaan kuva
                        });

                        // 2. Lisätään päätepiste (longitude = maxLng)
                        waypoints.Add(new Waypoint
                        {
                            Latitude = lat,  // Sama latitude
                            Longitude = maxLng,  // Itä (max longitude)
                            Altitude = currentAltitude,  // Sama altitude
                            Heading = -90,  // Länsi
                            GimbalAngle = angle,
                            Speed = speed,
                            Id = id++,
                            Action = allPointsAction  // Oletusarvoisesti otetaan kuva
                        });

                        // Päivitetään seuraavan vaiheen siirto, mutta **lisätään vain yksi piste korkeussuunnassa**
                        lat += latDistanceInDegrees;  // Siirrytään latitude-suunnassa
                        if (lat <= maxLat)
                        {
                            // 3. Siirrytään korkeussuunnassa (latitude) ja lisätään vain yksi piste
                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,  // Päivitetty latitude
                                Longitude = maxLng,  // Sama longitude (maxLng)
                                Altitude = currentAltitude,  // Sama altitude (korkeus)
                                Heading = 90,  // Itä
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction  // Oletusarvoisesti otetaan kuva
                            });

                            // 4. Takaisinpäin liike (länteen) ja lisätään vain yksi piste
                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,  // Sama latitude (palaamme samaan korkeuteen)
                                Longitude = minLng,  // Takaisin länteen (minLng)
                                Altitude = currentAltitude,  // Sama altitude
                                Heading = -90,  // Länsi
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction  // Oletusarvoisesti otetaan kuva
                            });
                        }

                        goingEast = !goingEast;  // Vaihdetaan suunta seuraavalle kierrokselle
                    }
                }
                else
                {
                    // Normaalilogiikka: Käytetään omaa silmukkaa, ei tehdä korkeussuunnan muutoksia tässä
                    for (double lat = minLat; lat <= maxLat; lat += (speed * photoInterval) / 111320.0)
                    {
                        double currentLng = goingEast ? minLng : maxLng;
                        double endLng = goingEast ? maxLng : minLng;
                        double step = goingEast ? (speed * photoInterval) / (111320.0 * Math.Cos(lat * Math.PI / 180.0))
                                                : -(speed * photoInterval) / (111320.0 * Math.Cos(lat * Math.PI / 180.0));

                        // Liikutaan vaakasuunnassa päätepisteiden välillä
                        while ((goingEast && currentLng <= endLng) || (!goingEast && currentLng >= endLng))
                        {
                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,
                                Longitude = currentLng,
                                Altitude = currentAltitude,  // Pidä korkeus samana vaakasuunnassa
                                Heading = goingEast ? 90 : -90,  // Itä-länsi suunta
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction  // Oletusarvoisesti otetaan kuva
                            });

                            currentLng += step;  // Päivitetään pituusaste seuraavaksi askeleeksi
                        }

                        // Päivitetään korkeus, kun saavutaan päätepisteeseen
                        currentAltitude += in_distance;
                        goingEast = !goingEast;
                    }
                }
            }
            else if (boundsType == "circle")
            {
                // Ympyräreittien generointi, ei käytetä in_distancea korkeuden muuttamiseen
                foreach (var bound in bounds)
                {
                    double centerLat = bound.Lat;
                    double centerLon = bound.Lng;
                    double radius = bound.Radius;

                    double circumference = 2 * Math.PI * radius;
                    double distancePerPhoto = speed * photoInterval;
                    int numberOfWaypoints = (int)(circumference / distancePerPhoto);

                    // Luo ympyrän reittipisteet samalla korkeudella
                    var circleWaypoints = await GenerateWaypointsForCircleAsync(centerLat, centerLon, radius, altitude, speed, allPointsAction, id, photoInterval);
                    waypoints.AddRange(circleWaypoints);
                }
            }

            return await Task.FromResult(waypoints);
        }

        // Method to generate waypoints for a circle
        private async Task<List<Waypoint>> GenerateWaypointsForCircleAsync(double centerLat, double centerLon, double radius, double altitude, double speed, string allPointsAction, int startingId, double photoInterval)
        {
            var waypoints = new List<Waypoint>();
            double circumference = 2 * Math.PI * radius;  // Circumference of the circle
            double distancePerPhoto = speed * photoInterval;  // Distance traveled per photo interval
            int numberOfWaypoints = (int)(circumference / distancePerPhoto);  // Calculate number of waypoints

            double angleStep = 360.0 / numberOfWaypoints;  // Divide the circle into equal angles
            double distanceCovered = 0;  // Track distance covered between waypoints
            int id = startingId;  // Initialize waypoint Id


            for (int i = 0; i < numberOfWaypoints; i++)
            {
                double angle = i * angleStep;  // Calculate the angle for the current waypoint

                // Convert angle to radians
                double angleRad = angle * (Math.PI / 180.0);

                // Calculate waypoint's latitude and longitude using the Haversine formula
                double waypointLat = centerLat + (radius / 111000.0) * Math.Cos(angleRad);  // Latitude displacement
                double waypointLon = centerLon + (radius / (111000.0 * Math.Cos(centerLat * Math.PI / 180.0))) * Math.Sin(angleRad);  // Longitude displacement

                if (distanceCovered >= distancePerPhoto)
                {
                    // Calculate heading towards the center of the circle
                    double deltaLat = centerLat - waypointLat;
                    double deltaLon = centerLon - waypointLon;
                    double heading = Math.Atan2(deltaLon, deltaLat) * (180.0 / Math.PI);  // Convert from radians to degrees

                    // Create and add the waypoint
                    waypoints.Add(new Waypoint
                    {
                        Latitude = waypointLat,
                        Longitude = waypointLon,
                        Altitude = altitude,
                        Heading = heading,  // Heading is towards the center
                        GimbalAngle = angle,
                        Speed = speed,
                        Id = id++,  // Increment Id for each waypoint
                        Action = allPointsAction
                    });

                    distanceCovered = 0;  // Reset distance covered after generating a waypoint
                }

                distanceCovered += (2 * Math.PI * radius) / numberOfWaypoints;  // Approximate the distance covered along the circle
            }

            return await Task.FromResult(waypoints);  // Return wrapped in Task
        }
    }

}
