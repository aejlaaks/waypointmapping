using System.Collections.Generic;
using System.Threading.Tasks;
using KarttaBackEnd2.Server.Controllers;
using KarttaBackEnd2.Server.DTOs;
using KarttaBackEnd2.Server.Interfaces;
using KarttaBackEnd2.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KarttaBackendTest
{
    public class WaypointControllerTests
    {
        private readonly Mock<IWaypointService> _mockWaypointService;
        private readonly WaypointsController _controller;

        public WaypointControllerTests()
        {
            _mockWaypointService = new Mock<IWaypointService>();
            _controller = new WaypointsController(_mockWaypointService.Object);
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
                new Waypoint { Id = 1, Latitude = 60.0, Longitude = 24.0 },
                new Waypoint { Id = 2, Latitude = 60.0, Longitude = 25.0 }
            };
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
            var result = await _controller.GeneratePoints(new GeneratePointsRequestDTO
            {
                AllPointsAction = "takePhoto",
                UnitType = 0,
                Altitude = 100,
                Speed = 10,
                Angle = 45,
                In_Distance = 100,
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
