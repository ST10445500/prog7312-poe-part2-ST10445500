using SmartX.Api.Services;

//ST10445500 - PROG7312 - SmartX POE
//StreamStatsTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the counters without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the alert and jumped packet totals add up, including when many threads record at once
    public class StreamStatsTests
    {
        [Fact]
        public void RecordAlert_OneAlert_AddsOneAndTheWaitingCount()
        {
            // Arrange
            var stats = new StreamStats();

            // Act
            stats.RecordAlert(12);

            // Assert
            Assert.Equal(1, stats.AlertsRaised);
            Assert.Equal(12, stats.PacketsJumped);
        }

        [Fact]
        public void RecordAlert_FromManyThreads_GivesExactTotals()
        {
            // Arrange
            var stats = new StreamStats();

            // Act
            Parallel.For(0, 5000, i => stats.RecordAlert(2));

            // Assert
            Assert.Equal(5000, stats.AlertsRaised);
            Assert.Equal(10000, stats.PacketsJumped);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
