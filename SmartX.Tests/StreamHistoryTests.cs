using SmartX.Api.Collections;
using SmartX.Api.Controllers;
using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//StreamHistoryTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the history route narrows by a range, and turns away a reversed range and a device never heard from
    public class StreamHistoryTests
    {
        private const string Mac = "AA:BB:CC:DD:EE:01";

        private static (StreamController Controller, DateTime Start) Build()
        {
            var cache = new DeviceCache();
            var log = new HistoryLog<HistoryPoint>(500);
            var controller = new StreamController(cache, new PacketQueue<StreamPacket>(), new CommandHistory<PumpCommand>(), new AlertQueue<StreamAlert>(), new StreamStats(), log, new ActiveFaults());

            var start = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

            cache.Update(new StreamPacket { MacAddress = Mac, Category = SensorCategory.Environmental, Value = 40, RecordedAt = start }, "Moisture");

            // One reading a minute for five minutes.
            for (var minute = 0; minute < 5; minute++)
            {
                var at = start.AddMinutes(minute);
                log.Add(Mac, at, new HistoryPoint { RecordedAt = at, Reading = "40%" });
            }

            return (controller, start);
        }

        [Fact]
        public void GetHistory_NoRange_ReturnsEverythingKept()
        {
            // Arrange
            var (controller, _) = Build();

            // Act
            var points = controller.GetHistory(Mac).Value!;

            // Assert
            Assert.Equal(5, points.Count);
        }

        [Fact]
        public void GetHistory_FromInUtc_ReturnsThePointsFromThen()
        {
            // Arrange
            var (controller, start) = Build();

            // Act
            var points = controller.GetHistory(Mac, start.AddMinutes(3)).Value!;

            // Assert
            Assert.Equal(2, points.Count);
        }

        [Fact]
        public void GetHistory_ToInUtc_ReturnsThePointsUpToThen()
        {
            // Arrange
            var (controller, start) = Build();

            // Act
            var points = controller.GetHistory(Mac, null, start.AddMinutes(1)).Value!;

            // Assert
            Assert.Equal(2, points.Count);
        }

        [Fact]
        public void GetHistory_FromAfterTo_ReturnsBadRequest()
        {
            // Arrange
            var (controller, start) = Build();

            // Act
            var result = controller.GetHistory(Mac, start.AddMinutes(3), start);

            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public void GetHistory_UnseenDevice_ReturnsNotFound()
        {
            // Arrange
            var (controller, _) = Build();

            // Act
            var result = controller.GetHistory("00:00:00:00:00:00");

            // Assert
            Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundObjectResult>(result.Result);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
