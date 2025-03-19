using System.Collections.Generic;
using System.Threading.Tasks;
using KarttaBackEnd2.Server.Models;
using KarttaBackEnd2.Server.Services;

using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KarttaBackendTest
{
    public class WaypointServiceTests
    {
        private readonly WaypointService _waypointService;
        private readonly Mock<ILogger<WaypointService>> _mockLogger;

        public WaypointServiceTests()
        {
            _mockLogger = new Mock<ILogger<WaypointService>>();
            _waypointService = new WaypointService(_mockLogger.Object);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_RectangleBounds_ReturnsWaypoints()
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
            var startingIndex = 1;

            // Act
            var waypoints = await _waypointService.GenerateWaypointsAsync(
                "takePhoto",
                0,
                100,
                10,
                0,
                100,
                bounds,
                boundsType,
                startingIndex,
                3,
                false,
                false
            );

            // Assert
            Assert.NotEmpty(waypoints);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_CircleBounds_ReturnsWaypoints()
        {
            // Arrange
            var bounds = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0, Radius = 1000 }
            };
            var boundsType = "circle";
            var startingIndex = 1;

            // Act
            var waypoints = await _waypointService.GenerateWaypointsAsync(
                "takePhoto",
                0,
                100,
                10,
                0,
                100,
                bounds,
                boundsType,
                startingIndex,
                3,
                false,
                false
            );

            // Assert
            Assert.NotEmpty(waypoints);
        }

        [Fact]
        public async Task GenerateWaypointsAsync_PolylineBounds_ReturnsWaypoints()
        {
            // Arrange
            var bounds = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.5, Lng = 24.5 },
                new Coordinate { Lat = 61.0, Lng = 25.0 }
            };
            var boundsType = "polyline";
            var startingIndex = 1;

            // Act
            var waypoints = await _waypointService.GenerateWaypointsAsync(
                "takePhoto",
                0,
                100,
                10,
                45,
                100,
                bounds,
                boundsType,
                startingIndex,
                3,
                false,
                false
            );

            // Assert
            Assert.NotEmpty(waypoints);
        }

        [Fact]
        public void CreateWaypoint_ReturnsWaypoint()
        {
            // Arrange
            double lat = 60.0;
            double lng = 24.0;
            double altitude = 100;
            double heading = 90;
            int angle = 45;
            double speed = 10;
            int id = 1;
            string action = "takePhoto";

            // Act
            var waypoint = _waypointService.CreateWaypoint(lat, lng, altitude, heading, angle, speed, ref id, action);

            // Assert
            Assert.NotNull(waypoint);
            Assert.Equal(lat, waypoint.Lat);
            Assert.Equal(lng, waypoint.Lng);
            Assert.Equal(altitude, waypoint.Alt);
            Assert.Equal(speed, waypoint.Speed);
            Assert.Equal(action, waypoint.Action);
        }

        [Fact]
        public void IsPointInPolygon_ReturnsTrueForPointInsidePolygon()
        {
            // Arrange
            var polygon = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 24.0 }
            };
            double lat = 60.5;
            double lng = 24.5;

            // Act
            var result = _waypointService.IsPointInPolygon(polygon, lat, lng);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPointInPolygon_ReturnsFalseForPointOutsidePolygon()
        {
            // Arrange
            var polygon = new List<Coordinate>
            {
                new Coordinate { Lat = 60.0, Lng = 24.0 },
                new Coordinate { Lat = 60.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 25.0 },
                new Coordinate { Lat = 61.0, Lng = 24.0 }
            };
            double lat = 62.0;
            double lng = 26.0;

            // Act
            var result = _waypointService.IsPointInPolygon(polygon, lat, lng);

            // Assert
            Assert.False(result);
        }
    }
}
