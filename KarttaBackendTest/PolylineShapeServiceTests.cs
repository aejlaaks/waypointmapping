using System.Collections.Generic;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;
using KarttaBackEnd2.Server.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KarttaBackendTest
{
    public class PolylineShapeServiceTests
    {
        private readonly PolylineShapeService _polylineService;
        private readonly Mock<ILogger<PolylineShapeService>> _mockLogger;
        private readonly Mock<IGeometryService> _mockGeometryService;

        public PolylineShapeServiceTests()
        {
            _mockLogger = new Mock<ILogger<PolylineShapeService>>();
            _mockGeometryService = new Mock<IGeometryService>();
            _polylineService = new PolylineShapeService(_mockGeometryService.Object, _mockLogger.Object);
        }

        [Fact]
        public void CanHandleShapeType_WithPolyline_ReturnsTrue()
        {
            // Act
            bool canHandle = _polylineService.CanHandleShapeType(ShapeTypes.Polyline);

            // Assert
            Assert.True(canHandle);
        }

        [Fact]
        public void CanHandleShapeType_WithOtherTypes_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(_polylineService.CanHandleShapeType(ShapeTypes.Rectangle));
            Assert.False(_polylineService.CanHandleShapeType(ShapeTypes.Circle));
            Assert.False(_polylineService.CanHandleShapeType(ShapeTypes.Polygon));
        }

        [Fact]
        public void GenerateWaypoints_WithPolyline_ReturnsWaypoints()
        {
            // Arrange
            var polyline = new ShapeData
            {
                Id = "1",
                Type = ShapeTypes.Polyline,
                Coordinates = new List<Coordinate>
                {
                    new Coordinate { Lat = 60.0, Lng = 24.0 },
                    new Coordinate { Lat = 60.5, Lng = 24.5 },
                    new Coordinate { Lat = 61.0, Lng = 25.0 }
                }
            };

            var parameters = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                StartingIndex = 1,
                PhotoInterval = 3,
                UseEndpointsOnly = false,
                IsNorthSouth = false,
                UnitType = 0
            };

            // Act
            var waypoints = _polylineService.GenerateWaypoints(polyline, parameters);

            // Assert
            Assert.NotEmpty(waypoints);
            // The polyline has 3 points, so with intermediate points we should have at least 3 waypoints
            Assert.True(waypoints.Count >= 3);
            
            // Check if first and last waypoints match the polyline endpoints
            Assert.Equal(60.0, waypoints[0].Lat);
            Assert.Equal(24.0, waypoints[0].Lng);
            Assert.Equal(61.0, waypoints[waypoints.Count - 1].Lat);
            Assert.Equal(25.0, waypoints[waypoints.Count - 1].Lng);
            
            // Check that all waypoints have the correct altitude and action
            foreach (var waypoint in waypoints)
            {
                Assert.Equal(100, waypoint.Alt);
                Assert.Equal("takePhoto", waypoint.Action);
                Assert.Equal(10, waypoint.Speed);
            }
        }

        [Fact]
        public void GenerateWaypoints_WithPolylineAndUseEndpointsOnly_ReturnsOnlyEndpoints()
        {
            // Arrange
            var polyline = new ShapeData
            {
                Id = "1",
                Type = ShapeTypes.Polyline,
                Coordinates = new List<Coordinate>
                {
                    new Coordinate { Lat = 60.0, Lng = 24.0 },
                    new Coordinate { Lat = 60.5, Lng = 24.5 },
                    new Coordinate { Lat = 61.0, Lng = 25.0 }
                }
            };

            var parameters = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                StartingIndex = 1,
                PhotoInterval = 3,
                UseEndpointsOnly = true, // Only use endpoints
                IsNorthSouth = false,
                UnitType = 0
            };

            // Act
            var waypoints = _polylineService.GenerateWaypoints(polyline, parameters);

            // Assert
            Assert.Equal(3, waypoints.Count); // Should have exactly the points in the polyline
            
            // Check if waypoints match the polyline coordinates
            Assert.Equal(60.0, waypoints[0].Lat);
            Assert.Equal(24.0, waypoints[0].Lng);
            Assert.Equal(60.5, waypoints[1].Lat);
            Assert.Equal(24.5, waypoints[1].Lng);
            Assert.Equal(61.0, waypoints[2].Lat);
            Assert.Equal(25.0, waypoints[2].Lng);
        }

        [Fact]
        public void GenerateWaypoints_WithEmptyPolyline_ReturnsEmptyList()
        {
            // Arrange
            var polyline = new ShapeData
            {
                Id = "1",
                Type = ShapeTypes.Polyline,
                Coordinates = new List<Coordinate>()
            };

            var parameters = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                StartingIndex = 1,
                PhotoInterval = 3,
                UseEndpointsOnly = false,
                IsNorthSouth = false,
                UnitType = 0
            };

            // Act
            var waypoints = _polylineService.GenerateWaypoints(polyline, parameters);

            // Assert
            Assert.Empty(waypoints);
        }

        [Fact]
        public void GenerateWaypoints_WithSinglePointPolyline_ReturnsSingleWaypoint()
        {
            // Arrange
            var polyline = new ShapeData
            {
                Id = "1",
                Type = ShapeTypes.Polyline,
                Coordinates = new List<Coordinate>
                {
                    new Coordinate { Lat = 60.0, Lng = 24.0 }
                }
            };

            var parameters = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                StartingIndex = 1,
                PhotoInterval = 3,
                UseEndpointsOnly = false,
                IsNorthSouth = false,
                UnitType = 0
            };

            // Act
            var waypoints = _polylineService.GenerateWaypoints(polyline, parameters);

            // Assert
            Assert.Single(waypoints);
            Assert.Equal(60.0, waypoints[0].Lat);
            Assert.Equal(24.0, waypoints[0].Lng);
            Assert.Equal(100, waypoints[0].Alt);
        }

        [Fact]
        public void GenerateWaypoints_ChecksCorrectSequentialIndices()
        {
            // Arrange
            var polyline = new ShapeData
            {
                Id = "1",
                Type = ShapeTypes.Polyline,
                Coordinates = new List<Coordinate>
                {
                    new Coordinate { Lat = 60.0, Lng = 24.0 },
                    new Coordinate { Lat = 60.5, Lng = 24.5 },
                    new Coordinate { Lat = 61.0, Lng = 25.0 }
                }
            };

            var parameters = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                StartingIndex = 5, // Start from index 5
                PhotoInterval = 3,
                UseEndpointsOnly = false,
                IsNorthSouth = false,
                UnitType = 0
            };

            // Act
            var waypoints = _polylineService.GenerateWaypoints(polyline, parameters);

            // Assert
            Assert.NotEmpty(waypoints);
            
            // Check that indices are sequential starting from 5
            for (int i = 0; i < waypoints.Count; i++)
            {
                Assert.Equal(i + 5, waypoints[i].Index);
            }
        }

        [Fact]
        public void GenerateWaypoints_WithHigherPhotoInterval_GeneratesFewerWaypoints()
        {
            // Arrange - Same polyline with different photo intervals
            var polyline = new ShapeData
            {
                Id = "1",
                Type = ShapeTypes.Polyline,
                Coordinates = new List<Coordinate>
                {
                    new Coordinate { Lat = 60.0, Lng = 24.0 },
                    new Coordinate { Lat = 61.0, Lng = 25.0 }
                }
            };

            var parametersLowInterval = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                StartingIndex = 1,
                PhotoInterval = 2, // Smaller interval = more waypoints
                UseEndpointsOnly = false,
                IsNorthSouth = false,
                UnitType = 0
            };

            var parametersHighInterval = new WaypointParameters
            {
                Action = "takePhoto",
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                StartingIndex = 1,
                PhotoInterval = 10, // Larger interval = fewer waypoints
                UseEndpointsOnly = false,
                IsNorthSouth = false,
                UnitType = 0
            };

            // Act
            var waypointsLowInterval = _polylineService.GenerateWaypoints(polyline, parametersLowInterval);
            var waypointsHighInterval = _polylineService.GenerateWaypoints(polyline, parametersHighInterval);

            // Assert
            Assert.True(waypointsLowInterval.Count > waypointsHighInterval.Count);
        }
    }
} 