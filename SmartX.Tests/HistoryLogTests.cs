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
        public void Constructor_ZeroCapacity_Throws()
        {
            // Arrange / Act
            var act = () => new HistoryLog<int>(0);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void Count_UnknownDevice_ReturnsZero()
        {
            // Arrange
            var log = new HistoryLog<int>(10);

            // Act
            var count = log.Count("AA:BB:CC:DD:EE:FF");

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetRange_UnknownDevice_ReturnsEmpty()
        {
            // Arrange
            var log = new HistoryLog<int>(10);

            // Act
            var range = log.GetRange("AA:BB:CC:DD:EE:FF", Start, Start.AddHours(1));

            // Assert
            Assert.Empty(range);
        }

        [Fact]
        public void GetRange_OneEntry_ReturnsIt()
        {
            // Arrange
            var log = new HistoryLog<int>(10);
            log.Add("dev1", Start, 42);

            // Act
            var range = log.GetRange("dev1", Start.AddMinutes(-1), Start.AddMinutes(1));

            // Assert
            Assert.Equal(new[] { 42 }, range);
            Assert.Equal(1, log.Count("dev1"));
        }

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
        public void GetRange_FromAfterTo_Throws()
        {
            // Arrange
            var log = new HistoryLog<int>(10);

            // Act
            var act = () => log.GetRange("dev1", Start.AddSeconds(5), Start);

            // Assert
            Assert.Throws<ArgumentException>(act);
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
        public void Add_OlderThanEverythingWhenFull_DropsTheNewEntryNotANewerOne()
        {
            // Arrange
            var log = new HistoryLog<int>(3);
            log.Add("dev1", Start.AddSeconds(10), 10);
            log.Add("dev1", Start.AddSeconds(11), 11);
            log.Add("dev1", Start.AddSeconds(12), 12);

            // Act
            log.Add("dev1", Start, 0);

            // Assert
            Assert.Equal(new[] { 10, 11, 12 }, log.GetRange("dev1", Start, Start.AddMinutes(1)));
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

        [Fact]
        public void Add_ManyDuplicatesOfOneTimestamp_KeepsAllInArrivalOrder()
        {
            // Arrange
            var log = new HistoryLog<int>(1000);

            // Act
            for (var i = 0; i < 500; i++)
            {
                log.Add("dev1", Start, i);
            }

            // Assert
            Assert.Equal(500, log.Count("dev1"));
            Assert.Equal(Enumerable.Range(0, 500), log.GetRange("dev1", Start, Start.AddSeconds(1)));
        }

        [Fact]
        public void GetRange_TwoDevices_DoNotMix()
        {
            // Arrange
            var log = new HistoryLog<int>(10);
            log.Add("dev1", Start, 1);
            log.Add("dev2", Start, 2);
            log.Add("dev2", Start.AddSeconds(1), 3);

            // Act
            var first = log.GetRange("dev1", Start, Start.AddMinutes(1));
            var second = log.GetRange("dev2", Start, Start.AddMinutes(1));

            // Assert
            Assert.Equal(new[] { 1 }, first);
            Assert.Equal(new[] { 2, 3 }, second);
            Assert.Equal(1, log.Count("dev1"));
            Assert.Equal(2, log.Count("dev2"));
        }

        [Fact]
        public void Add_FromManyThreads_KeepsEveryEntry()
        {
            // Arrange
            var log = new HistoryLog<int>(10000);

            // Act
            Parallel.For(0, 5000, i => log.Add("dev1", Start.AddSeconds(i), i));

            // Assert
            Assert.Equal(5000, log.Count("dev1"));
        }

        [Fact]
        public void GetRange_LargeShuffledLoad_ComesBackSorted()
        {
            // Arrange
            var log = new HistoryLog<int>(10000);
            var order = Enumerable.Range(0, 5000).ToArray();
            new Random(12345).Shuffle(order);

            foreach (var i in order)
            {
                log.Add("dev1", Start.AddSeconds(i), i);
            }

            // Act
            var range = log.GetRange("dev1", Start, Start.AddSeconds(5000));

            // Assert
            Assert.Equal(Enumerable.Range(0, 5000), range);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
