using System.Collections.Generic;
using System.Threading.Tasks;
using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;

using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KarttaBackendTest
{
    public class WaypointServiceV2Tests
    {
        private readonly WaypointServiceV2 _waypointServiceV2;
        private readonly Mock<ILogger<WaypointServiceV2>> _mockLogger;
        private readonly Mock<IShapeService> _mockRectangleShapeService;
        private readonly Mock<IShapeService> _mockCircleShapeService;
        private readonly Mock<IShapeService> _mockPolygonShapeService;
        private readonly Mock<IShapeService> _mockPolylineShapeService;

        public WaypointServiceV2Tests()
        {
            _mockLogger = new Mock<ILogger<WaypointServiceV2>>();
            
            // Setup shape services
            _mockRectangleShapeService = new Mock<IShapeService>();
            _mockRectangleShapeService.Setup(s => s.CanHandleShapeType(ShapeTypes.Rectangle)).Returns(true);
            
            _mockCircleShapeService = new Mock<IShapeService>();
            _mockCircleShapeService.Setup(s => s.CanHandleShapeType(ShapeTypes.Circle)).Returns(true);
            
            _mockPolygonShapeService = new Mock<IShapeService>();
            _mockPolygonShapeService.Setup(s => s.CanHandleShapeType(ShapeTypes.Polygon)).Returns(true);
            
            _mockPolylineShapeService = new Mock<IShapeService>();
            _mockPolylineShapeService.Setup(s => s.CanHandleShapeType(ShapeTypes.Polyline)).Returns(true);
            
            var shapeServices = new List<IShapeService> 
            { 
                _mockRectangleShapeService.Object,
                _mockCircleShapeService.Object, 
                _mockPolygonShapeService.Object,
                _mockPolylineShapeService.Object
            };
            
            _waypointServiceV2 = new WaypointServiceV2(shapeServices, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_WithRectangleShape_ReturnsWaypoints()
        {
            // Arrange
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
            
            var expectedWaypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.5, Lng = 24.5, Alt = 100, Speed = 10, Action = "takePhoto" }
            };
            
            _mockRectangleShapeService.Setup(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            )).Returns(expectedWaypoints);

            // Act
            var result = await _waypointServiceV2.GenerateWaypointsAsync(shapes, parameters);

            // Assert
            Assert.Equal(expectedWaypoints.Count, result.Count);
            Assert.Equal(expectedWaypoints[0].Lat, result[0].Lat);
            Assert.Equal(expectedWaypoints[0].Lng, result[0].Lng);
            _mockRectangleShapeService.Verify(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            ), Times.Once);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_WithCircleShape_ReturnsWaypoints()
        {
            // Arrange
            var shapes = new List<ShapeData>
            {
                new ShapeData
                {
                    Id = "1",
                    Type = ShapeTypes.Circle,
                    Coordinates = new List<Coordinate> { new Coordinate { Lat = 60.0, Lng = 24.0 } },
                    Radius = 1000
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
            
            var expectedWaypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.01, Lng = 24.01, Alt = 100, Speed = 10, Action = "takePhoto" }
            };
            
            _mockCircleShapeService.Setup(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            )).Returns(expectedWaypoints);

            // Act
            var result = await _waypointServiceV2.GenerateWaypointsAsync(shapes, parameters);

            // Assert
            Assert.Equal(expectedWaypoints.Count, result.Count);
            Assert.Equal(expectedWaypoints[0].Lat, result[0].Lat);
            Assert.Equal(expectedWaypoints[0].Lng, result[0].Lng);
            _mockCircleShapeService.Verify(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            ), Times.Once);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_WithPolygonShape_ReturnsWaypoints()
        {
            // Arrange
            var shapes = new List<ShapeData>
            {
                new ShapeData
                {
                    Id = "1",
                    Type = ShapeTypes.Polygon,
                    Coordinates = new List<Coordinate>
                    {
                        new Coordinate { Lat = 60.0, Lng = 24.0 },
                        new Coordinate { Lat = 60.0, Lng = 25.0 },
                        new Coordinate { Lat = 61.0, Lng = 25.0 },
                        new Coordinate { Lat = 61.0, Lng = 24.0 }
                    }
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
            
            var expectedWaypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.5, Lng = 24.5, Alt = 100, Speed = 10, Action = "takePhoto" }
            };
            
            _mockPolygonShapeService.Setup(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            )).Returns(expectedWaypoints);

            // Act
            var result = await _waypointServiceV2.GenerateWaypointsAsync(shapes, parameters);

            // Assert
            Assert.Equal(expectedWaypoints.Count, result.Count);
            Assert.Equal(expectedWaypoints[0].Lat, result[0].Lat);
            Assert.Equal(expectedWaypoints[0].Lng, result[0].Lng);
            _mockPolygonShapeService.Verify(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            ), Times.Once);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_WithPolylineShape_ReturnsWaypoints()
        {
            // Arrange
            var shapes = new List<ShapeData>
            {
                new ShapeData
                {
                    Id = "1",
                    Type = ShapeTypes.Polyline,
                    Coordinates = new List<Coordinate>
                    {
                        new Coordinate { Lat = 60.0, Lng = 24.0 },
                        new Coordinate { Lat = 60.5, Lng = 24.5 },
                        new Coordinate { Lat = 61.0, Lng = 25.0 }
                    }
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
            
            var expectedWaypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.5, Lng = 24.5, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 3, Lat = 61.0, Lng = 25.0, Alt = 100, Speed = 10, Action = "takePhoto" }
            };
            
            _mockPolylineShapeService.Setup(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            )).Returns(expectedWaypoints);

            // Act
            var result = await _waypointServiceV2.GenerateWaypointsAsync(shapes, parameters);

            // Assert
            Assert.Equal(expectedWaypoints.Count, result.Count);
            Assert.Equal(expectedWaypoints[0].Lat, result[0].Lat);
            Assert.Equal(expectedWaypoints[0].Lng, result[0].Lng);
            _mockPolylineShapeService.Verify(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            ), Times.Once);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_WithMultipleShapes_ReturnsAllWaypoints()
        {
            // Arrange
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
                    Type = ShapeTypes.Circle,
                    Coordinates = new List<Coordinate> { new Coordinate { Lat = 62.0, Lng = 26.0 } },
                    Radius = 1000
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
            
            var rectangleWaypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.5, Lng = 24.5, Alt = 100, Speed = 10, Action = "takePhoto" }
            };
            
            var circleWaypoints = new List<Waypoint>
            {
                new Waypoint { Index = 3, Lat = 62.0, Lng = 26.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 4, Lat = 62.01, Lng = 26.01, Alt = 100, Speed = 10, Action = "takePhoto" }
            };
            
            _mockRectangleShapeService.Setup(s => s.GenerateWaypoints(
                It.Is<ShapeData>(sd => sd.Id == "1"),
                It.IsAny<WaypointParameters>()
            )).Returns(rectangleWaypoints);
            
            _mockCircleShapeService.Setup(s => s.GenerateWaypoints(
                It.Is<ShapeData>(sd => sd.Id == "2"),
                It.IsAny<WaypointParameters>()
            )).Returns(circleWaypoints);

            // Act
            var result = await _waypointServiceV2.GenerateWaypointsAsync(shapes, parameters);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Equal(60.0, result[0].Lat);
            Assert.Equal(24.0, result[0].Lng);
            Assert.Equal(62.0, result[2].Lat);
            Assert.Equal(26.0, result[2].Lng);
            
            _mockRectangleShapeService.Verify(s => s.GenerateWaypoints(
                It.Is<ShapeData>(sd => sd.Id == "1"),
                It.IsAny<WaypointParameters>()
            ), Times.Once);
            
            _mockCircleShapeService.Verify(s => s.GenerateWaypoints(
                It.Is<ShapeData>(sd => sd.Id == "2"),
                It.IsAny<WaypointParameters>()
            ), Times.Once);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_WithEmptyShapes_ReturnsEmptyList()
        {
            // Arrange
            var shapes = new List<ShapeData>();
            
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
            var result = await _waypointServiceV2.GenerateWaypointsAsync(shapes, parameters);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_WithLegacyMethod_CorrectlyConvertsParameters()
        {
            // Arrange
            var bounds = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 24.0 }
            };
            
            var boundsType = "rectangle";
            var expectedWaypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.5, Lng = 24.5, Alt = 100, Speed = 10, Action = "takePhoto" }
            };
            
            _mockRectangleShapeService.Setup(s => s.GenerateWaypoints(
                It.IsAny<ShapeData>(),
                It.IsAny<WaypointParameters>()
            )).Returns(expectedWaypoints);

            // Act
            var result = await _waypointServiceV2.GenerateWaypointsAsync(
                "takePhoto",
                0,
                100,
                10,
                0, // Angle is no longer used
                100,
                bounds,
                boundsType,
                1,
                3,
                false,
                false
            );

            // Assert
            Assert.Equal(expectedWaypoints.Count, result.Count);
            _mockRectangleShapeService.Verify(s => s.GenerateWaypoints(
                It.Is<ShapeData>(sd => sd.Type == ShapeTypes.Rectangle),
                It.Is<WaypointParameters>(p => 
                    p.Altitude == 100 && 
                    p.Speed == 10 && 
                    p.LineSpacing == 100 &&
                    p.StartingIndex == 1 &&
                    p.PhotoInterval == 3 &&
                    p.UseEndpointsOnly == false &&
                    p.IsNorthSouth == false &&
                    p.UnitType == 0
                )
            ), Times.Once);
        }
    }
} 