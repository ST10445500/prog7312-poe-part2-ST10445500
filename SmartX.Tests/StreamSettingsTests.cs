using Microsoft.Extensions.Configuration;
using SmartX.Api.Models.Stream;

//ST10445500 - PROG7312 - SmartX POE
//StreamSettingsTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the settings without needing the API to be running.

namespace SmartX.Tests
{
    //checks the defaults, that a bad value is refused by name and that the Stream section binds over the defaults
    public class StreamSettingsTests
    {
        [Fact]
        public void Defaults_NothingChanged_MatchTheOriginalValues()
        {
            // Arrange / Act
            var settings = new StreamSettings();

            // Assert
            Assert.Equal(15, settings.LowMoisturePercent);
            Assert.Equal(600, settings.HighWatts);
            Assert.Equal(-90, settings.WeakRssi);
            Assert.Equal(15, settings.SilentSeconds);
            Assert.Equal(1000, settings.SilentCheckMilliseconds);
            Assert.Equal(50, settings.IdleMilliseconds);
            Assert.Equal(250, settings.PushMilliseconds);
            Assert.Equal(500, settings.HistoryCap);
        }

        [Fact]
        public void Validate_Defaults_DoesNotThrow()
        {
            // Arrange
            var settings = new StreamSettings();

            // Act
            var exception = Record.Exception(() => settings.Validate());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_ValidChange_DoesNotThrow()
        {
            // Arrange
            var settings = new StreamSettings { HighWatts = 300, WeakRssi = -80, HistoryCap = 10 };

            // Act
            var exception = Record.Exception(() => settings.Validate());

            // Assert
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validate_MoistureOutsideZeroToHundred_ThrowsNamingTheSetting(double value)
        {
            // Arrange
            var settings = new StreamSettings { LowMoisturePercent = value };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:LowMoisturePercent", exception.Message);
        }

        [Fact]
        public void Validate_ZeroHighWatts_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { HighWatts = 0 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:HighWatts", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Validate_WeakRssiNotNegative_ThrowsNamingTheSetting(int value)
        {
            // Arrange
            var settings = new StreamSettings { WeakRssi = value };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:WeakRssi", exception.Message);
        }

        [Fact]
        public void Validate_ZeroSilentSeconds_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { SilentSeconds = 0 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:SilentSeconds", exception.Message);
        }

        [Fact]
        public void Validate_ZeroSilentCheckMilliseconds_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { SilentCheckMilliseconds = 0 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:SilentCheckMilliseconds", exception.Message);
        }

        [Fact]
        public void Validate_ZeroIdleMilliseconds_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { IdleMilliseconds = 0 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:IdleMilliseconds", exception.Message);
        }

        [Fact]
        public void Validate_ZeroPushMilliseconds_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { PushMilliseconds = 0 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:PushMilliseconds", exception.Message);
        }

        [Fact]
        public void Validate_ZeroHistoryCap_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { HistoryCap = 0 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:HistoryCap", exception.Message);
        }

        [Fact]
        public void Validate_NotANumberForTheMoistureLimit_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { LowMoisturePercent = double.NaN };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:LowMoisturePercent", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void Validate_MoistureLimitOnTheEdge_Passes(double percent)
        {
            // Arrange
            var settings = new StreamSettings { LowMoisturePercent = percent };

            // Act
            var exception = Record.Exception(() => settings.Validate());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_HugeSilentSeconds_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { SilentSeconds = int.MaxValue };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:SilentSeconds", exception.Message);
        }

        [Fact]
        public void Validate_HugeHistoryCap_ThrowsNamingTheSetting()
        {
            // Arrange
            var settings = new StreamSettings { HistoryCap = int.MaxValue };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:HistoryCap", exception.Message);
        }

        [Fact]
        public void Validate_SilentLimitNotLongerThanTheCheck_Throws()
        {
            // Arrange
            var settings = new StreamSettings { SilentSeconds = 5, SilentCheckMilliseconds = 5000 };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());

            // Assert
            Assert.Contains("Stream:SilentSeconds", exception.Message);
        }

        [Fact]
        public void Get_OneValueInTheSection_ChangesThatValueAndKeepsTheRestDefault()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Stream:HighWatts"] = "300" })
                .Build();

            // Act
            var settings = configuration.GetSection("Stream").Get<StreamSettings>();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(300, settings.HighWatts);
            Assert.Equal(15, settings.LowMoisturePercent);
            Assert.Equal(-90, settings.WeakRssi);
            Assert.Equal(15, settings.SilentSeconds);
            Assert.Equal(1000, settings.SilentCheckMilliseconds);
            Assert.Equal(50, settings.IdleMilliseconds);
            Assert.Equal(250, settings.PushMilliseconds);
            Assert.Equal(500, settings.HistoryCap);
        }

        [Fact]
        public void Get_NoStreamSection_ReturnsNull()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();

            // Act
            var settings = configuration.GetSection("Stream").Get<StreamSettings>();

            // Assert
            Assert.Null(settings);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
