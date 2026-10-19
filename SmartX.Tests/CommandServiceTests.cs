using SmartX.Api.Collections;
using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//CommandServiceTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that a sent override is remembered with the state it replaced and that undo puts that state back newest first
    public class CommandServiceTests
    {
        private const string MacA = "AA:BB:CC:DD:EE:01";
        private const string MacB = "AA:BB:CC:DD:EE:02";

        private const string MacC = "AA:BB:CC:DD:EE:03";
        private const string MacMoisture = "AA:BB:CC:DD:EE:04";

        private static (CommandService Service, DeviceCache Cache, PacketQueue<StreamPacket> Queue, CommandHistory<PumpCommand> History) Build()
        {
            var (service, cache, queue, history, _) = BuildWithSensors();
            return (service, cache, queue, history);
        }

        private static (CommandService Service, DeviceCache Cache, PacketQueue<StreamPacket> Queue, CommandHistory<PumpCommand> History, SensorStore Sensors) BuildWithSensors()
        {
            var cache = new DeviceCache();
            var queue = new PacketQueue<StreamPacket>();
            var history = new CommandHistory<PumpCommand>();
            var sensors = new SensorStore();
            var service = new CommandService(new TelemetryStore(), cache, queue, history, sensors, new PendingCommands(), new InsightService(sensors, new ActiveFaults()));
            return (service, cache, queue, history, sensors);
        }

        private static void Register(SensorStore sensors, string mac, SensorCategory category)
        {
            sensors.TryRegister(new SensorRegistration
            {
                MacAddress = mac,
                Category = category,
                Location = new DeploymentLocation { Zone = "Zone A", Room = "Room 1", NodeId = "Node " + mac }
            });
        }

        private static void SeedValve(DeviceCache cache, string mac, bool open)
        {
            cache.Update(new StreamPacket
            {
                MacAddress = mac,
                Category = SensorCategory.Actuator,
                State = open,
                RecordedAt = DateTime.UtcNow
            }, "Valve");
        }

        private static List<StreamPacket> Drain(PacketQueue<StreamPacket> queue)
        {
            var packets = new List<StreamPacket>();
            while (queue.TryDequeue(out var packet))
            {
                packets.Add(packet!);
            }
            return packets;
        }

        [Fact]
        public void TryUndo_EmptyHistory_ReturnsFalseAndNull()
        {
            // Arrange
            var (service, _, _, _) = Build();

            // Act
            var undone = service.TryUndo(out var command);

            // Assert
            Assert.False(undone);
            Assert.Null(command);
        }

        [Fact]
        public void TryUndo_AfterShutAll_ReopensOnlyTheOnesThatWereOpen()
        {
            // Arrange
            var (service, cache, queue, history, sensors) = BuildWithSensors();
            Register(sensors, MacA, SensorCategory.Actuator);
            Register(sensors, MacB, SensorCategory.Actuator);
            Register(sensors, MacC, SensorCategory.Actuator);
            SeedValve(cache, MacA, true);
            SeedValve(cache, MacB, true);
            SeedValve(cache, MacC, false);
            service.ShutAll();
            Drain(queue);

            // Act
            var undone = service.TryUndo(out var command);
            var packets = Drain(queue);

            // Assert
            Assert.True(undone);
            Assert.Equal(2, command!.Changes.Count);
            Assert.Equal(2, packets.Count);
            Assert.All(packets, packet => Assert.True(packet.State));
            Assert.Contains(packets, packet => packet.MacAddress == MacA);
            Assert.Contains(packets, packet => packet.MacAddress == MacB);
            Assert.DoesNotContain(packets, packet => packet.MacAddress == MacC);
            Assert.Equal(0, history.Count);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
