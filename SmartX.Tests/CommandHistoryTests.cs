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
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
