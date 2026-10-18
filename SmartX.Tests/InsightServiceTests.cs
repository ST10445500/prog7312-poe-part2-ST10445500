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
        public void GetSuggestions_FreshStart_HasNothingToSuggest()
        {
            // Arrange
            var (service, _) = Build();

            // Act
            var suggestions = service.GetSuggestions();

            // Assert
            Assert.Empty(suggestions);
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
        public void GetSuggestions_AfterShutAllAndUndo_WordsEachInPlainEnglish()
        {
            // Arrange
            var (service, _) = Build();
            service.RecordShutAll();
            service.RecordUndo();
            service.RecordShutAll();

            // Act
            var top = service.GetSuggestions()[0];

            // Assert
            Assert.Equal("Undo the last override", top.Text);
            Assert.Equal("After shutting all valves, this came next 1 of 1 time.", top.Reason);
        }

        [Fact]
        public void RecordSearch_BlankOrRepeated_IsIgnored()
        {
            // Arrange
            var (service, _) = Build();

            // Act
            var blank = service.RecordSearch("   ");
            var first = service.RecordSearch("Soil");
            var again = service.RecordSearch("soil ");

            // Assert
            Assert.False(blank);
            Assert.True(first);
            Assert.False(again);
        }

        [Fact]
        public void RecordSearch_VeryLongText_IsCutShort()
        {
            // Arrange
            var (service, _) = Build();
            service.RecordAlert("Anything");
            service.RecordSearch(new string('x', 500));
            service.RecordAlert("Anything");

            // Act
            var top = service.GetSuggestions()[0];

            // Assert
            Assert.Equal(64, top.Text.Length - "Search for ".Length);
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

        [Fact]
        public void GetSuggestions_UnregisteredDevice_FallsBackToItsMac()
        {
            // Arrange
            var (service, _) = Build();
            service.RecordCommand("00:11:22:33:44:55", true);
            service.RecordCommand("00:11:22:33:44:55", false);
            service.RecordCommand("00:11:22:33:44:55", true);

            // Act
            var top = service.GetSuggestions()[0];

            // Assert
            Assert.Equal("Close 00:11:22:33:44:55", top.Text);
            Assert.Equal("After you opened 00:11:22:33:44:55, this came next 1 of 1 time.", top.Reason);
        }

        [Fact]
        public void RecordAlert_ManyThreads_StillAnswers()
        {
            // Arrange
            var (service, _) = Build();

            // Act
            Parallel.For(0, 2000, index =>
            {
                service.RecordAlert("HighWatts");
                service.RecordSearch("s" + index % 5);
            });

            // Assert
            Assert.NotNull(service.GetSuggestions());
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
