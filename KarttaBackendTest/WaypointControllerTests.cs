using System.Collections.Generic;
using System.Threading.Tasks;
using KarttaBackEnd2.Server.Controllers;
using KarttaBackEnd2.Server.DTOs;
using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KarttaBackendTest
{
    public class WaypointControllerTests
    {
        private readonly Mock<IWaypointService> _mockWaypointService;
        private readonly Mock<ILogger<WaypointsController>> _mockLogger;
        private readonly WaypointsController _controller;

        public WaypointControllerTests()
        {
            _mockWaypointService = new Mock<IWaypointService>();
            _mockLogger = new Mock<ILogger<WaypointsController>>();
            _controller = new WaypointsController(_mockWaypointService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateWaypoints_ReturnsOkResult_WithWaypoints()
        {
            // Arrange
            var bounds = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 24.0 }
            };
            var waypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.0, Lng = 25.0, Alt = 100, Speed = 10, Action = "takePhoto" }
            };

            // Setup the mock for the legacy method
            _mockWaypointService.Setup(s => s.GenerateWaypointsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<List<Coordinate>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()
            )).ReturnsAsync(waypoints);

            // Act
            var result = await _controller.GenerateWaypoints(new GeneratePointsRequestDTO
            {
                AllPointsAction = "takePhoto",
                UnitType = 0,
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                Bounds = bounds,
                BoundsType = "rectangle",
                StartingIndex = 1,
                PhotoInterval = 3,
                UseEndpointsOnly = false,
                IsNorthSouth = false
            });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<Waypoint>>(okResult.Value);
            Assert.Equal(waypoints.Count, returnValue.Count);
        }

        [Fact]
        public async Task GeneratePointsV2_ReturnsOkResult_WithWaypoints()
        {
            // Arrange
            var bounds = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 24.0 }
            };
            var waypoints = new List<Waypoint>
            {
                new Waypoint { Index = 1, Lat = 60.0, Lng = 24.0, Alt = 100, Speed = 10, Action = "takePhoto" },
                new Waypoint { Index = 2, Lat = 60.0, Lng = 25.0, Alt = 100, Speed = 10, Action = "takePhoto" }
            };

            // Setup the mock to handle the new interface method with shapes and parameters
            _mockWaypointService.Setup(s => s.GenerateWaypointsAsync(
                It.IsAny<List<ShapeData>>(),
                It.IsAny<WaypointParameters>()
            )).ReturnsAsync(waypoints);

            // Act
            var result = await _controller.GeneratePointsV2(new GeneratePointsRequestDTO
            {
                AllPointsAction = "takePhoto",
                UnitType = 0,
                Altitude = 100,
                Speed = 10,
                LineSpacing = 100,
                Bounds = bounds,
                BoundsType = "rectangle",
                StartingIndex = 1,
                PhotoInterval = 3,
                UseEndpointsOnly = false,
                IsNorthSouth = false
            });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<Waypoint>>(okResult.Value);
            Assert.Equal(waypoints.Count, returnValue.Count);
        }
    }
}
