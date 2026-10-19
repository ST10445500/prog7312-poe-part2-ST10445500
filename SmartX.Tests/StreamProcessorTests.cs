using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using SmartX.Api.Collections;
using SmartX.Api.Hubs;
using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//StreamProcessorTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the processor takes an alert before the ordinary packets behind it and marks a quiet device
    public class StreamProcessorTests
    {
        private const string MacA = "AA:BB:CC:DD:EE:01";
        private const string MacB = "AA:BB:CC:DD:EE:02";

        // The page push is not what these tests are about, so the hub just fails and the processor has to carry on.
        private class FailingHub : IHubContext<StreamHub>
        {
            public IHubClients Clients => throw new InvalidOperationException("No page is listening.");

            public IGroupManager Groups => throw new InvalidOperationException("No page is listening.");
        }

        private class Parts
        {
            public PacketQueue<StreamPacket> Queue { get; } = new();
            public AlertQueue<StreamAlert> Alerts { get; } = new();
            public DeviceCache Cache { get; } = new();
            public StreamStats Stats { get; } = new();
            public ActiveFaults Faults { get; } = new();
            public HistoryLog<HistoryPoint> History { get; } = new(500);

            public StreamProcessor Build()
            {
                var sensors = new SensorStore();

                return new StreamProcessor(Queue, Cache, sensors, Alerts, Stats, History, Faults, new AlertRules(), new FailingHub(),
                    NullLogger<StreamProcessor>.Instance, new StreamSettings(), new InsightService(sensors, Faults));
            }
        }

        private static StreamPacket Moisture(string mac, double percent)
        {
            return new StreamPacket
            {
                MacAddress = mac,
                Category = SensorCategory.Environmental,
                Value = percent,
                RecordedAt = DateTime.UtcNow
            };
        }

        // Waits for a condition for up to five seconds so a slow machine does not fail the test by a hair.
        private static async Task<bool> WaitForAsync(Func<bool> condition)
        {
            for (var i = 0; i < 500; i++)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(10);
            }

            return false;
        }

        [Fact]
        public async Task Processor_AlertBehindOrdinaryPackets_JumpsEveryOneOfThem()
        {
            // Arrange
            var parts = new Parts();
            for (var i = 0; i < 200; i++)
            {
                parts.Queue.Enqueue(Moisture(MacA, 50));
            }
            var packet = Moisture(MacB, 5);
            parts.Alerts.Enqueue(new StreamAlert { Packet = packet, Rule = "LowMoisture", Severity = StreamAlert.Critical }, StreamAlert.Critical);
            var processor = parts.Build();

            // Act
            await processor.StartAsync(CancellationToken.None);
            var drained = await WaitForAsync(() => parts.Queue.Count == 0 && parts.Cache.Find(MacA) != null);
            await processor.StopAsync(CancellationToken.None);

            // Assert
            Assert.True(drained);
            Assert.Equal(1, parts.Stats.AlertsRaised);
            Assert.Equal(200, parts.Stats.PacketsJumped);
            Assert.NotNull(parts.Cache.Find(MacB));
            Assert.Equal(200, parts.History.Count(MacA));
            Assert.Equal(1, parts.History.Count(MacB));
        }

        [Fact]
        public async Task Processor_DeviceNotHeardFromForTooLong_IsMarkedDisconnected()
        {
            // Arrange
            var parts = new Parts();
            var old = Moisture(MacA, 40);
            old.RecordedAt = DateTime.UtcNow.AddMinutes(-5);
            parts.Cache.Update(old, "N01");
            var processor = parts.Build();

            // Act
            await processor.StartAsync(CancellationToken.None);
            var marked = await WaitForAsync(() => parts.Faults.GetDisconnected().Contains(MacA));
            await processor.StopAsync(CancellationToken.None);

            // Assert
            Assert.True(marked);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
