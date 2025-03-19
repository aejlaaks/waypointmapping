using System.Collections.Generic;
using System.Threading.Tasks;



using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;

namespace KarttaBackendTest
{
    public class IntegrationTests
    {
        private readonly IServiceProvider _serviceProvider;

        public IntegrationTests()
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            
            // Register required services
            services.AddLogging(configure => configure.AddConsole());
            services.AddScoped<IWaypointService, WaypointService>();
            services.AddScoped<WaypointServiceV2>();
            services.AddScoped<WaypointServiceAdapter>();
            
            // Register shape services
            services.AddScoped<IShapeService, RectangleShapeService>();
            services.AddScoped<IShapeService, CircleShapeService>();
            services.AddScoped<IShapeService, PolygonShapeService>();
            services.AddScoped<IShapeService, PolylineShapeService>();
            
            // Register geometry service
            var geometryServiceMock = new Mock<IGeometryService>();
            geometryServiceMock.Setup(g => g.CalculateDistance(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns<double, double, double, double>((lat1, lng1, lat2, lng2) => {
                    // Simple Haversine formula implementation for testing
                    const double earthRadius = 6371000; // meters
                    double dLat = (lat2 - lat1) * Math.PI / 180.0;
                    double dLng = (lng2 - lng1) * Math.PI / 180.0;
                    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                               Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                               Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
                    double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                    return earthRadius * c;
                });
            
            geometryServiceMock.Setup(g => g.MetersToDegrees(It.IsAny<double>(), It.IsAny<double>()))
                .Returns<double, double>((meters, latitude) => {
                    // Convert meters to degrees at the given latitude
                    double earthRadius = 6371000; // meters
                    double latDegrees = (meters / earthRadius) * (180.0 / Math.PI);
                    double lngDegrees = (meters / (earthRadius * Math.Cos(latitude * Math.PI / 180.0))) * (180.0 / Math.PI);
                    return Math.Max(latDegrees, lngDegrees); // Return the larger value for testing purposes
                });
            
            services.AddSingleton(geometryServiceMock.Object);
            
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Polyline_WaypointGeneration_EndToEnd_Test()
        {
            // Arrange
            var waypointService = _serviceProvider.GetRequiredService<IWaypointService>();
            
            // Create a simple polyline
            var polylineCoordinates = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.5, Lng = 24.5 },
                new Coordinate { Lat = 61.0, Lng = 25.0 }
            };
            
            // Act
            var waypoints = await waypointService.GenerateWaypointsAsync(
                "takePhoto",
                0, // Metric units
                100, // 100m altitude
                10, // 10 m/s speed
                0, // Unused angle parameter
                50, // 50m line spacing
                polylineCoordinates,
                "polyline",
                1, // Starting index
                3, // Photo every 3 seconds
                false, // Don't use endpoints only
                false // East-west pattern (though irrelevant for polyline)
            );
            
            // Assert
            Assert.NotNull(waypoints);
            Assert.NotEmpty(waypoints);
            
            // Verify we have at least the endpoints
            Assert.True(waypoints.Count >= 3, $"Expected at least 3 waypoints, got {waypoints.Count}");
            
            // Check the first and last waypoints match the polyline endpoints
            Assert.Equal(60.0, waypoints[0].Lat);
            Assert.Equal(24.0, waypoints[0].Lng);
            
            Assert.Equal(61.0, waypoints[waypoints.Count - 1].Lat);
            Assert.Equal(25.0, waypoints[waypoints.Count - 1].Lng);
            
            // Check waypoint properties
            foreach (var waypoint in waypoints)
            {
                Assert.Equal(100, waypoint.Alt);
                Assert.Equal(10, waypoint.Speed);
                Assert.Equal("takePhoto", waypoint.Action);
            }
        }

        [Fact]
        public async Task Polyline_UseEndpointsOnly_GeneratesWaypointsOnlyAtVertices()
        {
            // Arrange
            var waypointService = _serviceProvider.GetRequiredService<IWaypointService>();
            
            // Create a simple polyline
            var polylineCoordinates = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.5, Lng = 24.5 },
                new Coordinate { Lat = 61.0, Lng = 25.0 }
            };
            
            // Act - with useEndpointsOnly = true
            var waypoints = await waypointService.GenerateWaypointsAsync(
                "takePhoto",
                0,
                100,
                10,
                0,
                50,
                polylineCoordinates,
                "polyline",
                1,
                3,
                true, // Use endpoints only
                false
            );
            
            // Assert
            Assert.NotNull(waypoints);
            Assert.Equal(3, waypoints.Count);
            
            // Verify the waypoints are at the exact polyline vertices
            Assert.Equal(60.0, waypoints[0].Lat);
            Assert.Equal(24.0, waypoints[0].Lng);
            
            Assert.Equal(60.5, waypoints[1].Lat);
            Assert.Equal(24.5, waypoints[1].Lng);
            
            Assert.Equal(61.0, waypoints[2].Lat);
            Assert.Equal(25.0, waypoints[2].Lng);
        }

        [Fact]
        public async Task ShapeData_WaypointGeneration_WithMultipleShapes()
        {
            // Arrange
            var waypointServiceV2 = _serviceProvider.GetRequiredService<WaypointServiceV2>();
            
            // Create two shapes (rectangle and polyline)
            var shapes = new List<ShapeData>
            {
                new ShapeData
                {
                    Id = "1",
                    Type = ShapeTypes.Rectangle,
                    Coordinates = new List<Coordinate>
                    {
                        new Coordinate { Lat = 60.0, Lng = 24.0 },
                        new Coordinate { Lat = 61.0, Lng = 25.0 }
                    }
                },
                new ShapeData
                {
                    Id = "2",
                    Type = ShapeTypes.Polyline,
                    Coordinates = new List<Coordinate>
                    {
                        new Coordinate { Lat = 62.0, Lng = 26.0 },
                        new Coordinate { Lat = 62.5, Lng = 26.5 },
                        new Coordinate { Lat = 63.0, Lng = 27.0 }
                    }
                }
            };
            
            var parameters = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 50,
                StartingIndex = 1,
                PhotoInterval = 3,
                UseEndpointsOnly = false,
                IsNorthSouth = false,
                UnitType = 0
            };
            
            // Act - use WaypointServiceV2 directly since it properly handles multiple shapes
            var waypoints = await waypointServiceV2.GenerateWaypointsAsync(shapes, parameters);
            
            // Assert
            Assert.NotNull(waypoints);
            Assert.NotEmpty(waypoints);
            
            // Verify we have waypoints from both shapes
            bool foundRectangleWaypoint = waypoints.Any(w => w.Lat >= 60.0 && w.Lat <= 61.0 && w.Lng >= 24.0 && w.Lng <= 25.0);
            bool foundPolylineWaypoint = waypoints.Any(w => w.Lat >= 62.0 && w.Lat <= 63.0 && w.Lng >= 26.0 && w.Lng <= 27.0);
            
            Assert.True(foundRectangleWaypoint, "No waypoints found from the rectangle shape");
            Assert.True(foundPolylineWaypoint, "No waypoints found from the polyline shape");
        }
    }
} 