using SmartX.Api.Collections;
using SmartX.Api.Controllers;
using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//StreamFilterTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the device filters narrow by search, category and alerts together, show faults and quiet nodes, and leave the cache alone
    public class StreamFilterTests
    {
        private const string MacMoisture = "AA:BB:CC:DD:EE:01";
        private const string MacValve = "AA:BB:CC:DD:EE:02";
        private const string MacMeter = "AA:BB:CC:DD:EE:03";

        private static (StreamController Controller, DeviceCache Cache, ActiveFaults Faults) Build()
        {
            var cache = new DeviceCache();
            var faults = new ActiveFaults();
            var controller = new StreamController(cache, new PacketQueue<StreamPacket>(), new CommandHistory<PumpCommand>(), new AlertQueue<StreamAlert>(), new StreamStats(), new HistoryLog<HistoryPoint>(500), faults);

            Seed(cache, MacMoisture, SensorCategory.Environmental, "Moisture");
            Seed(cache, MacMeter, SensorCategory.PowerConsumption, "Meter");
            Seed(cache, MacValve, SensorCategory.Actuator, "Valve");

            return (controller, cache, faults);
        }

        private static void Seed(DeviceCache cache, string mac, SensorCategory category, string name)
        {
            cache.Update(new StreamPacket
            {
                MacAddress = mac,
                Category = category,
                Value = 40,
                State = category == SensorCategory.Actuator ? true : null,
                RecordedAt = DateTime.UtcNow
            }, name);
        }

        [Fact]
        public void GetDevices_NoFilters_ReturnsAllThree()
        {
            // Arrange
            var (controller, _, _) = Build();

            // Act
            var devices = controller.GetDevices().Value!;

            // Assert
            Assert.Equal(3, devices.Count);
        }

        [Fact]
        public void GetDevices_CategoryActuator_ReturnsOnlyTheActuator()
        {
            // Arrange
            var (controller, _, _) = Build();

            // Act
            var devices = controller.GetDevices(null, SensorCategory.Actuator).Value!;

            // Assert
            var device = Assert.Single(devices);
            Assert.Equal(MacValve, device.MacAddress);
        }

        [Fact]
        public void GetDevices_AlertsOnlyNothingWrong_ReturnsEmpty()
        {
            // Arrange
            var (controller, _, _) = Build();

            // Act
            var devices = controller.GetDevices(null, null, true).Value!;

            // Assert
            Assert.Empty(devices);
        }

        [Fact]
        public void GetDevices_AlertsOnlyWithFaultAndQuietNode_ReturnsExactlyThoseTwo()
        {
            // Arrange
            var (controller, _, faults) = Build();
            faults.Report(MacValve, "StuckValve");
            faults.MarkDisconnected(MacMeter);

            // Act
            var devices = controller.GetDevices(null, null, true).Value!;

            // Assert
            Assert.Equal(2, devices.Count);
            Assert.Contains(devices, device => device.MacAddress == MacValve);
            Assert.Contains(devices, device => device.MacAddress == MacMeter);
        }

        [Fact]
        public void GetDevices_CategoryAndAlertsOnly_ReturnsTheIntersection()
        {
            // Arrange
            var (controller, _, faults) = Build();
            faults.Report(MacValve, "StuckValve");
            faults.MarkDisconnected(MacMeter);

            // Act
            var devices = controller.GetDevices(null, SensorCategory.Actuator, true).Value!;

            // Assert
            var device = Assert.Single(devices);
            Assert.Equal(MacValve, device.MacAddress);
        }

        [Fact]
        public void GetDevices_SearchAndCategory_ReturnsTheIntersection()
        {
            // Arrange
            var (controller, _, _) = Build();

            // Act
            var match = controller.GetDevices("EE:02", SensorCategory.Actuator).Value!;
            var clash = controller.GetDevices("EE:02", SensorCategory.Environmental).Value!;

            // Assert
            Assert.Single(match);
            Assert.Empty(clash);
        }

        [Fact]
        public void GetDevices_SearchMatchesNothing_ReturnsEmpty()
        {
            // Arrange
            var (controller, _, _) = Build();

            // Act
            var devices = controller.GetDevices("FF:FF").Value!;

            // Assert
            Assert.Empty(devices);
        }

        [Fact]
        public void GetDevices_FaultAndQuietNode_CarriesTheCodeOnlyOnTheFaultyOneAndQuietOnlyOnTheQuietOne()
        {
            // Arrange
            var (controller, _, faults) = Build();
            faults.Report(MacValve, "StuckValve");
            faults.MarkDisconnected(MacMeter);

            // Act
            var devices = controller.GetDevices().Value!;

            // Assert
            var valve = devices.Single(device => device.MacAddress == MacValve);
            var meter = devices.Single(device => device.MacAddress == MacMeter);
            var moisture = devices.Single(device => device.MacAddress == MacMoisture);
            Assert.Equal(new[] { "StuckValve" }, valve.Faults);
            Assert.False(valve.Disconnected);
            Assert.True(meter.Disconnected);
            Assert.Empty(meter.Faults);
            Assert.False(moisture.Disconnected);
            Assert.Empty(moisture.Faults);
        }

        [Fact]
        public void GetDevices_AfterFaultCleared_SecondResultHasNoFaultsAndCacheWasNeverChanged()
        {
            // Arrange
            var (controller, cache, faults) = Build();
            faults.Report(MacValve, "StuckValve");
            var first = controller.GetDevices().Value!;

            // Act
            faults.Report(MacValve, null);
            var second = controller.GetDevices().Value!;

            // Assert
            Assert.Single(first.Single(device => device.MacAddress == MacValve).Faults);
            Assert.Empty(second.Single(device => device.MacAddress == MacValve).Faults);
            Assert.Empty(cache.Find(MacValve)!.Faults);
        }

        [Fact]
        public void GetSnapshot_WithCategory_NarrowsDevicesButActiveErrorsStaysTheTotal()
        {
            // Arrange
            var (controller, _, faults) = Build();
            faults.Report(MacValve, "StuckValve");
            faults.Report(MacMeter, "Overload");

            // Act
            var snapshot = controller.GetSnapshot(null, SensorCategory.Actuator).Value!;

            // Assert
            Assert.Single(snapshot.Devices);
            Assert.Equal(2, snapshot.ActiveErrors);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
