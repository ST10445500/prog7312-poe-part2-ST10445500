using SmartX.Api.Models.Telemetry;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//TelemetryHealthTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the classifier buckets readings correctly and tells a spike from an ordinary reading
    public class TelemetryHealthTests
    {
        private const string KnownMac = "AA:BB:CC:DD:EE:01";

        // Five seconds past midnight sits inside a ten second grid line, not on one.
        private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 5, DateTimeKind.Utc);
        private static readonly DateTime FlooredNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime FirstWindowStart =
            FlooredNow.AddSeconds(-(TelemetryHealth.DefaultWindowCount - 1) * TelemetryHealth.DefaultWindowSeconds);

        private static (TelemetryHealth Health, TelemetryStore Store, SensorRegistration Sensor) BuildHealth()
        {
            var store = new TelemetryStore();
            var sensor = new SensorRegistration
            {
                MacAddress = KnownMac,
                Category = SensorCategory.PowerConsumption,
                Location = new DeploymentLocation { Zone = "Zone 1", Room = "Sub-Zone B", NodeId = "N01" }
            };

            return (new TelemetryHealth(store), store, sensor);
        }

        [Fact]
        public void Build_WithAReadingAtTheFirstWindowStart_PlacesItInWindowZero()
        {
            // Arrange
            var (health, store, sensor) = BuildHealth();
            store.RecordPower(KnownMac, 100, FirstWindowStart);

            // Act
            var result = health.Build(sensor, TelemetryHealth.DefaultWindowCount, TelemetryHealth.DefaultWindowSeconds, Now);

            // Assert
            Assert.Equal(1, result.Windows[0].Count);
            Assert.All(result.Windows, (window, index) =>
            {
                if (index != 0)
                {
                    Assert.Equal(0, window.Count);
                }
            });
        }

        [Fact]
        public void Build_WithAJumpPastTheThreshold_FlagsThatWindowButNotASmallerOne()
        {
            // Arrange
            var (health, store, sensor) = BuildHealth();

            var secondToLastWindowStart = FirstWindowStart.AddSeconds((TelemetryHealth.DefaultWindowCount - 2) * TelemetryHealth.DefaultWindowSeconds);
            var lastWindowStart = FirstWindowStart.AddSeconds((TelemetryHealth.DefaultWindowCount - 1) * TelemetryHealth.DefaultWindowSeconds);

            // An ordinary window: two readings 10W apart, well under the 50W threshold.
            store.RecordPower(KnownMac, 100, secondToLastWindowStart.AddSeconds(1));
            store.RecordPower(KnownMac, 110, secondToLastWindowStart.AddSeconds(2));

            // A spiking window: two readings 100W apart.
            store.RecordPower(KnownMac, 100, lastWindowStart.AddSeconds(1));
            store.RecordPower(KnownMac, 200, lastWindowStart.AddSeconds(2));

            // Act
            var result = health.Build(sensor, TelemetryHealth.DefaultWindowCount, TelemetryHealth.DefaultWindowSeconds, Now);

            // Assert
            Assert.Equal(TelemetryStatus.Normal, result.Windows[^2].Status);
            Assert.Equal(TelemetryStatus.Spike, result.Windows[^1].Status);
            Assert.Equal(TelemetryStatus.Spike, result.Status);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
