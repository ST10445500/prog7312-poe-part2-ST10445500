using SmartX.Api.Collections;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//InsightServiceTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the engine suggests nothing before anything happens, learns from alerts, commands and searches, words its reasons plainly and flags problem devices first
    public class InsightServiceTests
    {
        private const string PumpA = "AA:BB:CC:DD:EE:02";
        private const string PumpB = "AA:BB:CC:DD:EE:06";
        private const string SoilB = "AA:BB:CC:DD:EE:05";

        private static (InsightService Service, ActiveFaults Faults) Build()
        {
            var sensors = new SensorStore();
            DemoFleet.Seed(sensors);
            var faults = new ActiveFaults();
            return (new InsightService(sensors, faults), faults);
        }

        [Fact]
        public void GetSuggestions_AfterSeededAlert_SuggestsTheUsualValve()
        {
            // Arrange
            var (service, _) = Build();

            // Act
            service.RecordAlert("LowMoisture");
            var top = service.GetSuggestions()[0];

            // Assert
            Assert.Equal("Next", top.Kind);
            Assert.Equal("Open valve-a1", top.Text);
            Assert.Equal(PumpA, top.MacAddress);
            Assert.True(top.Open);
            Assert.Equal(5.0 / 6.0, top.Confidence);
            Assert.Equal("After a LowMoisture alert, this came next 5 of 6 times.", top.Reason);
        }

        [Fact]
        public void GetSuggestions_AfterSeededSearch_SuggestsTheOtherValve()
        {
            // Arrange
            var (service, _) = Build();

            // Act
            service.RecordSearch("  AA:BB:CC:DD:EE:05 ");
            var top = service.GetSuggestions()[0];

            // Assert
            Assert.Equal("Open valve-b1", top.Text);
            Assert.Equal(1.0, top.Confidence);
            Assert.Equal("After searching for aa:bb:cc:dd:ee:05, this came next 3 of 3 times.", top.Reason);
        }

        [Fact]
        public void GetSuggestions_RepeatedPattern_OutranksTheSeed()
        {
            // Arrange
            var (service, _) = Build();

            // Act
            for (var i = 0; i < 10; i++)
            {
                service.RecordAlert("LowMoisture");
                service.RecordCommand(PumpB, true);
            }

            service.RecordAlert("LowMoisture");
            var top = service.GetSuggestions()[0];

            // Assert
            Assert.Equal("Open valve-b1", top.Text);
        }

        [Fact]
        public void GetSuggestions_FaultOnADevice_ComesFirstAsAProblem()
        {
            // Arrange
            var (service, faults) = Build();
            service.RecordAlert("LowMoisture");
            faults.TryRaise(PumpA, "StuckValve");

            // Act
            var suggestions = service.GetSuggestions();

            // Assert
            Assert.Equal("Problem", suggestions[0].Kind);
            Assert.Equal("Check valve-a1", suggestions[0].Text);
            Assert.Equal("valve-a1 is reporting StuckValve.", suggestions[0].Reason);
            Assert.Equal("Next", suggestions[1].Kind);
        }

        [Fact]
        public void GetSuggestions_SilentDevice_ComesFirstAsAProblem()
        {
            // Arrange
            var (service, faults) = Build();
            faults.MarkDisconnected(SoilB);

            // Act
            var suggestions = service.GetSuggestions();

            // Assert
            Assert.Single(suggestions);
            Assert.Equal("Problem", suggestions[0].Kind);
            Assert.Equal("soil-b1 has stopped reporting.", suggestions[0].Reason);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
