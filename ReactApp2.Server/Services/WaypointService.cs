using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using SharpKml.Dom;
using Waypoint = KarttaBackEnd2.Server.Models.Waypoint;

namespace KarttaBackEnd2.Server.Services
{
    public class WaypointService : IWaypointService
    {
         private async Task<List<Waypoint>> GenerateWaypointsForCircleAsync(double centerLat, double centerLon, double semiMajorAxis, double semiMinorAxis, double altitude, double speed, string allPointsAction, int startingId, double photoInterval)
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





        public async Task<List<Waypoint>> GenerateWaypointsAsync(
    string allPointsAction,
    int unitType_in,
    double altitude,
    double speed,  // Nopeus metreinä sekunnissa
    int angle,
    double in_distance,
    List<Coordinate> bounds,  // Suorakulmion koordinaatit
    string boundsType,
    int in_startingIndex,
    double photoInterval = 3,
    bool useEndpointsOnly = false,  // Kontrolloi päätepisteiden logiikkaa
    bool isNorthSouth = false  // Uusi parametri suunnan valintaan: true = Pohjoinen-Etelä, false = Itä-Länsi
)
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

                double currentAltitude = altitude;  // Korkeus pysyy vakiona
                //isNorthSouth = false;
                // Valitaan suunta
                if (!isNorthSouth)  // Itä-Länsi-suunnassa
                {
                    double latDistanceInDegrees = in_distance / 111320.0;  // Muunnetaan metreiksi leveysasteina
                    bool goingEast = true;  // Pitää yllä suunnan

                    if (useEndpointsOnly)
                    {
                        for (double lat = minLat; lat <= maxLat; lat += latDistanceInDegrees)
                        {
                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,
                                Longitude = minLng,  // Länsi (min longitude)
                                Altitude = currentAltitude,
                                Heading = 90,  // Itään
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            // 2. Lisätään päätepiste (longitude = maxLng)
                            waypoints.Add(new Waypoint
                            {
                                Latitude = lat,  // Sama latitude
                                Longitude = maxLng,  // Itä (max longitude)
                                Altitude = currentAltitude,
                                Heading = 0,  // Länsi (maxLng)
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            // Siirrytään seuraavaan latitude-tasoon
                            lat += latDistanceInDegrees;
                            if (lat <= maxLat)
                            {
                                // 3. Siirrytään korkeussuunnassa ja lisätään yksi piste
                                waypoints.Add(new Waypoint
                                {
                                    Latitude = lat,
                                    Longitude = maxLng,  // Sama longitude (maxLng)
                                    Altitude = currentAltitude,
                                    Heading = 270,  // Itään
                                    GimbalAngle = angle,
                                    Speed = speed,
                                    Id = id++,
                                    Action = allPointsAction
                                });

                                // 4. Takaisinpäin liike (länteen) ja lisätään yksi piste
                                waypoints.Add(new Waypoint
                                {
                                    Latitude = lat,
                                    Longitude = minLng,  // Takaisin länteen (minLng)
                                    Altitude = currentAltitude,
                                    Heading = 0,  // Länsi
                                    GimbalAngle = angle,
                                    Speed = speed,
                                    Id = id++,
                                    Action = allPointsAction
                                });
                            }

                            // Vaihdetaan suunta seuraavalle kierrokselle
                            goingEast = !goingEast;
                        }
                    }
                }
                else  // Pohjoinen-Etelä-suunnassa
                {
                    double lngDistanceInDegrees = in_distance / (111320.0 * Math.Cos(minLat * Math.PI / 180.0));  // Muunnetaan metreiksi pituusasteina
                    bool goingNorth = true;  // Pitää yllä suunnan

                    if (useEndpointsOnly)
                    {
                        for (double lng = minLng; lng <= maxLng; lng += lngDistanceInDegrees)
                        {
                            // 1. Lisätään lähtöpiste (latitude = minLat)
                            waypoints.Add(new Waypoint
                            {
                                Latitude = minLat,  // Etelä (min latitude)
                                Longitude = lng,
                                Altitude = currentAltitude,
                                Heading = 0,  // Pohjoiseen
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            // 2. Lisätään päätepiste (latitude = maxLat)
                            waypoints.Add(new Waypoint
                            {
                                Latitude = maxLat,  // Pohjoinen (max latitude)
                                Longitude = lng,
                                Altitude = currentAltitude,
                                Heading = 180,  // Etelään
                                GimbalAngle = angle,
                                Speed = speed,
                                Id = id++,
                                Action = allPointsAction
                            });

                            // Päivitetään seuraavan vaiheen siirto, mutta **lisätään vain yksi piste pituussuunnassa**
                            lng += lngDistanceInDegrees;  // Siirrytään longitude-suunnassa
                            if (lng <= maxLng)
                            {
                                // 3. Siirrytään pituussuunnassa (longitude) ja lisätään vain yksi piste
                                waypoints.Add(new Waypoint
                                {
                                    Latitude = maxLat,  // Sama latitude (maxLat)
                                    Longitude = lng,  // Päivitetty longitude
                                    Altitude = currentAltitude,
                                    Heading = 360,  // Pohjoiseen
                                    GimbalAngle = angle,
                                    Speed = speed,
                                    Id = id++,
                                    Action = allPointsAction
                                });

                                // 4. Takaisinpäin liike (etelään) ja lisätään vain yksi piste
                                waypoints.Add(new Waypoint
                                {
                                    Latitude = minLat,  // Sama latitude (minLat)
                                    Longitude = lng,  // Sama longitude (lng)
                                    Altitude = currentAltitude,
                                    Heading = 180,  // Etelään
                                    GimbalAngle = angle,
                                    Speed = speed,
                                    Id = id++,
                                    Action = allPointsAction
                                });
                            }

                            // Vaihdetaan suunta seuraavalle kierrokselle
                            goingNorth = !goingNorth;
                        }
                    }
                }
            }
            else if (boundsType == "circle")
            {
                // Ympyräreittien generointi
                foreach (var bound in bounds)
                {
                    double centerLat = bound.Lat;
                    double centerLon = bound.Lng;
                    double semiMajorAxis = bound.Radius; // Ellipsin suuri akseli (käytetään Radius-nimeä)
                    double semiMinorAxis = bound.Radius / 2; // Ellipsin pieni akseli, oletetaan se puolikkaaksi suurimmasta akselista

                    double circumference = 2 * Math.PI * Math.Sqrt((semiMajorAxis * semiMajorAxis + semiMinorAxis * semiMinorAxis) / 2);  // Ellipsin arvioitu kehä
                    double distancePerPhoto = speed * photoInterval;
                    int numberOfWaypoints = (int)(circumference / distancePerPhoto);

                    // Luo ellipsin reittipisteet samalla korkeudella
                    var ellipseWaypoints = await GenerateWaypointsForCircleAsync(centerLat, centerLon, semiMajorAxis, semiMinorAxis, altitude, speed, allPointsAction, id, photoInterval);
                    waypoints.AddRange(ellipseWaypoints);
                }
            }

            return await Task.FromResult(waypoints);
        }

    }

}
