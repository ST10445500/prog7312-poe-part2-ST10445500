using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//AlertRulesTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the rules without needing the API to be running.

namespace SmartX.Tests
{
    //checks that each threshold fires on the right side of the line and that normal readings raise nothing
    public class AlertRulesTests
    {
        private static StreamPacket Packet(SensorCategory category, double? value, int? rssi = null)
        {
            return new StreamPacket
            {
                MacAddress = "AA:BB:CC:DD:EE:01",
                Category = category,
                Value = value,
                RecordedAt = DateTime.UtcNow,
                Radio = new RadioStats { Rssi = rssi }
            };
        }

        [Fact]
        public void Evaluate_MoistureBelowLimit_RaisesCriticalLowMoisture()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Environmental, 14.9);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.NotNull(alert);
            Assert.Equal("LowMoisture", alert.Rule);
            Assert.Equal(StreamAlert.Critical, alert.Severity);
        }

        [Fact]
        public void Evaluate_MoistureExactlyAtLimit_ReturnsNull()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Environmental, 15);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.Null(alert);
        }

        [Fact]
        public void Evaluate_WattsAboveLimit_RaisesCriticalHighWatts()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.PowerConsumption, 600.5);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.NotNull(alert);
            Assert.Equal("HighWatts", alert.Rule);
            Assert.Equal(StreamAlert.Critical, alert.Severity);
        }

        [Fact]
        public void Evaluate_WattsExactlyAtLimit_ReturnsNull()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.PowerConsumption, 600);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.Null(alert);
        }

        [Fact]
        public void Evaluate_RssiBelowLimit_RaisesWarningWeakSignal()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Environmental, 40, -91);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.NotNull(alert);
            Assert.Equal("WeakSignal", alert.Rule);
            Assert.Equal(StreamAlert.Warning, alert.Severity);
        }

        [Fact]
        public void Evaluate_RssiExactlyAtLimit_ReturnsNull()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Environmental, 40, -90);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.Null(alert);
        }

        [Fact]
        public void Evaluate_NoRssiAndNormalValue_ReturnsNull()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Environmental, 40);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.Null(alert);
        }

        [Fact]
        public void Evaluate_ActuatorWithNormalRssi_ReturnsNull()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Actuator, null, -60);
            packet.State = true;

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.Null(alert);
        }

        [Fact]
        public void Evaluate_DryMoistureAndWeakSignal_MoistureWins()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Environmental, 5, -95);

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.NotNull(alert);
            Assert.Equal("LowMoisture", alert.Rule);
        }

        [Fact]
        public void Evaluate_ValveReportsFault_RaisesCriticalNamedAfterTheCode()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Actuator, null);
            packet.Fault = "StuckValve";

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.NotNull(alert);
            Assert.Equal("StuckValve", alert.Rule);
            Assert.Equal(StreamAlert.Critical, alert.Severity);
        }

        [Fact]
        public void Evaluate_FaultAndWeakSignal_FaultWins()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Actuator, null, -95);
            packet.Fault = "StuckValve";

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.NotNull(alert);
            Assert.Equal("StuckValve", alert.Rule);
        }

        [Fact]
        public void FindSilent_EmptyList_ReturnsNothing()
        {
            // Arrange
            var rules = new AlertRules();

            // Act
            var silent = rules.FindSilent(new List<DeviceState>(), DateTime.UtcNow);

            // Assert
            Assert.Empty(silent);
        }

        [Fact]
        public void FindSilent_JustInsideTheLimit_IsNotSilent()
        {
            // Arrange
            var rules = new AlertRules();
            var now = DateTime.UtcNow;
            var devices = new List<DeviceState>
            {
                new DeviceState { MacAddress = "AA:BB:CC:DD:EE:01", LastSeen = now.AddSeconds(-(new StreamSettings().SilentSeconds - 1)) }
            };

            // Act
            var silent = rules.FindSilent(devices, now);

            // Assert
            Assert.Empty(silent);
        }

        [Fact]
        public void FindSilent_PastTheLimit_ReturnsOnlyThatDevice()
        {
            // Arrange
            var rules = new AlertRules();
            var now = DateTime.UtcNow;
            var devices = new List<DeviceState>
            {
                new DeviceState { MacAddress = "AA:BB:CC:DD:EE:01", LastSeen = now.AddSeconds(-(new StreamSettings().SilentSeconds + 1)) },
                new DeviceState { MacAddress = "AA:BB:CC:DD:EE:02", LastSeen = now.AddSeconds(-2) }
            };

            // Act
            var silent = rules.FindSilent(devices, now);

            // Assert
            Assert.Equal(new[] { "AA:BB:CC:DD:EE:01" }, silent);
        }

        [Fact]
        public void Evaluate_LoweredHighWatts_RaisesForAReadingTheDefaultAllows()
        {
            // Arrange
            var defaultRules = new AlertRules();
            var lowered = new AlertRules(new StreamSettings { HighWatts = 300 });
            var packet = Packet(SensorCategory.PowerConsumption, 350);

            // Act
            var byDefault = defaultRules.Evaluate(packet);
            var alert = lowered.Evaluate(packet);

            // Assert
            Assert.Null(byDefault);
            Assert.NotNull(alert);
            Assert.Equal("HighWatts", alert.Rule);
        }

        [Fact]
        public void Evaluate_RaisedLowMoisture_RaisesForAReadingTheDefaultAllows()
        {
            // Arrange
            var defaultRules = new AlertRules();
            var raised = new AlertRules(new StreamSettings { LowMoisturePercent = 30 });
            var packet = Packet(SensorCategory.Environmental, 25);

            // Act
            var byDefault = defaultRules.Evaluate(packet);
            var alert = raised.Evaluate(packet);

            // Assert
            Assert.Null(byDefault);
            Assert.NotNull(alert);
            Assert.Equal("LowMoisture", alert.Rule);
        }

        [Fact]
        public void FindSilent_ShorterSilentSeconds_FlagsADeviceTheDefaultLeavesAlone()
        {
            // Arrange
            var defaultRules = new AlertRules();
            var shorter = new AlertRules(new StreamSettings { SilentSeconds = 5 });
            var now = DateTime.UtcNow;
            var devices = new List<DeviceState>
            {
                new DeviceState { MacAddress = "AA:BB:CC:DD:EE:01", LastSeen = now.AddSeconds(-10) }
            };

            // Act
            var byDefault = defaultRules.FindSilent(devices, now);
            var silent = shorter.FindSilent(devices, now);

            // Assert
            Assert.Empty(byDefault);
            Assert.Equal(new[] { "AA:BB:CC:DD:EE:01" }, silent);
        }

        [Fact]
        public void Evaluate_BlankFault_IsIgnored()
        {
            // Arrange
            var rules = new AlertRules();
            var packet = Packet(SensorCategory.Actuator, null);
            packet.Fault = "   ";

            // Act
            var alert = rules.Evaluate(packet);

            // Assert
            Assert.Null(alert);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
