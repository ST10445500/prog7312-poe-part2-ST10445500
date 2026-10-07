using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DeviceCacheTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the cache finds a device by mac address and keeps only the latest reading
    public class DeviceCacheTests
    {
        private static StreamPacket Moisture(string mac, double percent)
        {
            return new StreamPacket
            {
                MacAddress = mac,
                Category = SensorCategory.Environmental,
                Value = percent,
                RecordedAt = DateTime.UtcNow,
                Radio = new RadioStats { Rssi = -60, HopCount = 2, Protocol = "Zigbee" }
            };
        }

        [Fact]
        public void Find_UnknownMac_ReturnsNull()
        {
            // Arrange
            var cache = new DeviceCache();

            // Act
            var state = cache.Find("AA:BB:CC:DD:EE:01");

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void Find_LowerCaseMac_FindsTheDevice()
        {
            // Arrange
            var cache = new DeviceCache();
            cache.Update(Moisture("AA:BB:CC:DD:EE:01", 42.5), "N01");

            // Act
            var state = cache.Find("aa:bb:cc:dd:ee:01");

            // Assert
            Assert.NotNull(state);
            Assert.Equal("42.5%", state.Reading);
            Assert.Equal(-60, state.Radio.Rssi);
        }

        [Fact]
        public void Update_SameMacTwice_KeepsTheLatestOnly()
        {
            // Arrange
            var cache = new DeviceCache();

            // Act
            cache.Update(Moisture("AA:BB:CC:DD:EE:01", 40), "N01");
            cache.Update(Moisture("AA:BB:CC:DD:EE:01", 55), "N01");

            // Assert
            Assert.Single(cache.GetAll());
            Assert.Equal("55%", cache.Find("AA:BB:CC:DD:EE:01")!.Reading);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
