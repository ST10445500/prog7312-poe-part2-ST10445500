using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//HistoryLogTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that a device history stays in time order, keeps duplicates and drops the oldest past the cap
    public class HistoryLogTests
    {
        private static readonly DateTime Start = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void GetRange_OutOfOrderInserts_ComesBackOldestFirst()
        {
            // Arrange
            var log = new HistoryLog<int>(10);
            log.Add("dev1", Start.AddSeconds(30), 3);
            log.Add("dev1", Start.AddSeconds(10), 1);
            log.Add("dev1", Start.AddSeconds(20), 2);

            // Act
            var range = log.GetRange("dev1", Start, Start.AddMinutes(1));

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, range);
        }

        [Fact]
        public void GetRange_BoundsOnEntries_IncludesBothEndsAndExcludesOutside()
        {
            // Arrange
            var log = new HistoryLog<int>(10);

            for (var i = 0; i < 5; i++)
            {
                log.Add("dev1", Start.AddSeconds(i), i);
            }

            // Act
            var range = log.GetRange("dev1", Start.AddSeconds(1), Start.AddSeconds(3));

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, range);
        }

        [Fact]
        public void Add_PastCapacity_DropsTheOldest()
        {
            // Arrange
            var log = new HistoryLog<int>(3);

            // Act
            for (var i = 0; i < 5; i++)
            {
                log.Add("dev1", Start.AddSeconds(i), i);
            }

            // Assert
            Assert.Equal(3, log.Count("dev1"));
            Assert.Equal(new[] { 2, 3, 4 }, log.GetRange("dev1", Start, Start.AddMinutes(1)));
        }

        [Fact]
        public void Add_DuplicateTimestamp_KeepsBothInArrivalOrder()
        {
            // Arrange
            var log = new HistoryLog<string>(10);

            // Act
            var first = log.Add("dev1", Start, "first");
            var second = log.Add("dev1", Start, "second");

            // Assert
            Assert.Equal(Start, first);
            Assert.True(second > first);
            Assert.Equal(new[] { "first", "second" }, log.GetRange("dev1", Start, Start.AddSeconds(1)));
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
