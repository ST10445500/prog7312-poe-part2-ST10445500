using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//CommandHistoryTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that undo hands back the newest override first
    public class CommandHistoryTests
    {
        [Fact]
        public void TryPop_EmptyHistory_ReturnsFalse()
        {
            // Arrange
            var history = new CommandHistory<string>();

            // Act
            var found = history.TryPop(out _);

            // Assert
            Assert.False(found);
        }

        [Fact]
        public void TryPop_AfterTwoPushes_ReturnsTheLastOneFirst()
        {
            // Arrange
            var history = new CommandHistory<string>();
            history.Push("first");
            history.Push("second");

            // Act
            history.TryPop(out var newest);
            history.TryPop(out var older);

            // Assert
            Assert.Equal("second", newest);
            Assert.Equal("first", older);
            Assert.Equal(0, history.Count);
        }

        [Fact]
        public void GetAll_EmptyHistory_ReturnsEmptyList()
        {
            // Arrange
            var history = new CommandHistory<string>();

            // Act
            var all = history.GetAll();

            // Assert
            Assert.Empty(all);
        }

        [Fact]
        public void GetAll_AfterTwoPushes_ReturnsNewestFirstAndKeepsTheCount()
        {
            // Arrange
            var history = new CommandHistory<string>();
            history.Push("first");
            history.Push("second");

            // Act
            var all = history.GetAll();

            // Assert
            Assert.Equal(new[] { "second", "first" }, all);
            Assert.Equal(2, history.Count);
        }

        [Fact]
        public void GetAll_PushAfterwards_EarlierListDoesNotChange()
        {
            // Arrange
            var history = new CommandHistory<string>();
            history.Push("first");
            var all = history.GetAll();

            // Act
            history.Push("second");

            // Assert
            Assert.Single(all);
            Assert.Equal("first", all[0]);
        }

        [Fact]
        public void GetAll_ThousandPushes_KeepsNewestFirstOrder()
        {
            // Arrange
            var history = new CommandHistory<int>();
            for (var i = 0; i < 1000; i++)
            {
                history.Push(i);
            }

            // Act
            var all = history.GetAll();

            // Assert
            Assert.Equal(1000, all.Count);
            for (var i = 0; i < 1000; i++)
            {
                Assert.Equal(999 - i, all[i]);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
