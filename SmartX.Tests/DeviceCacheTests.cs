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
        public void Update_FromManyThreads_KeepsEveryDevice()
        {
            // Arrange
            var cache = new DeviceCache();

            // Act
            Parallel.For(0, 2000, i =>
            {
                cache.Update(Moisture($"AA:BB:CC:00:{i / 256:X2}:{i % 256:X2}", 40), "N" + i);
            });

            // Assert
            Assert.Equal(2000, cache.GetAll().Count);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
