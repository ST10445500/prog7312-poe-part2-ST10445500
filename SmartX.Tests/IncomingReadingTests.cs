using System.Text.Json;
using SmartX.Api.Models.Telemetry;

//ST10445500 - PROG7312 - SmartX POE
//IncomingReadingTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the radio fields are optional, so readings from before they existed still bind
    public class IncomingReadingTests
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        [Fact]
        public void Deserialize_WithoutRadioFields_LeavesThemNull()
        {
            // Arrange
            var json = """{"macAddress":"AA:BB:CC:DD:EE:01","value":42.5}""";

            // Act
            var reading = JsonSerializer.Deserialize<IncomingReading>(json, Options)!;

            // Assert
            Assert.Equal(42.5, reading.Value);
            Assert.Null(reading.Rssi);
            Assert.Null(reading.HopCount);
            Assert.Null(reading.Protocol);
        }

        [Fact]
        public void Deserialize_WithRadioFields_ReadsThem()
        {
            // Arrange
            var json = """{"macAddress":"AA:BB:CC:DD:EE:01","value":42.5,"rssi":-71,"hopCount":2,"dutyCycle":1.5,"packetLoss":0.8,"protocol":"Thread"}""";

            // Act
            var reading = JsonSerializer.Deserialize<IncomingReading>(json, Options)!;

            // Assert
            Assert.Equal(-71, reading.Rssi);
            Assert.Equal(2, reading.HopCount);
            Assert.Equal(1.5, reading.DutyCycle);
            Assert.Equal(0.8, reading.PacketLoss);
            Assert.Equal("Thread", reading.Protocol);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
