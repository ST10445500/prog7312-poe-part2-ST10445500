using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//PendingCommandsTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that a waiting command is handed over once, that the newest one wins, that devices stay apart and that it holds up under many threads
    public class PendingCommandsTests
    {
        private const string MacA = "AA:BB:CC:DD:EE:01";
        private const string MacB = "AA:BB:CC:DD:EE:02";

        [Fact]
        public void TryTake_Empty_ReturnsFalse()
        {
            // Arrange
            var pending = new PendingCommands();

            // Act
            var found = pending.TryTake(MacA, out _);

            // Assert
            Assert.False(found);
            Assert.Equal(0, pending.Count);
        }

        [Fact]
        public void TryTake_OneCommandSet_ReturnsItAndEmptiesIt()
        {
            // Arrange
            var pending = new PendingCommands();
            pending.Set(MacA, true);

            // Act
            var found = pending.TryTake(MacA, out var open);

            // Assert
            Assert.True(found);
            Assert.True(open);
            Assert.Equal(0, pending.Count);
        }

        [Fact]
        public void TryTake_SecondTime_ReturnsFalse()
        {
            // Arrange
            var pending = new PendingCommands();
            pending.Set(MacA, false);
            pending.TryTake(MacA, out _);

            // Act
            var found = pending.TryTake(MacA, out _);

            // Assert
            Assert.False(found);
        }

        [Fact]
        public void Set_TwiceForTheSameDevice_KeepsTheLatest()
        {
            // Arrange
            var pending = new PendingCommands();
            pending.Set(MacA, true);
            pending.Set(MacA, false);

            // Act
            pending.TryTake(MacA, out var open);

            // Assert
            Assert.False(open);
            Assert.Equal(0, pending.Count);
        }

        [Fact]
        public void TryTake_DifferentCase_FindsTheSameDevice()
        {
            // Arrange
            var pending = new PendingCommands();
            pending.Set("aa:bb:cc:dd:ee:01", true);

            // Act
            var found = pending.TryTake(MacA, out var open);

            // Assert
            Assert.True(found);
            Assert.True(open);
        }

        [Fact]
        public void TryTake_TwoDevices_TakingOneLeavesTheOther()
        {
            // Arrange
            var pending = new PendingCommands();
            pending.Set(MacA, true);
            pending.Set(MacB, false);

            // Act
            pending.TryTake(MacA, out _);
            var foundB = pending.TryTake(MacB, out var openB);

            // Assert
            Assert.True(foundB);
            Assert.False(openB);
            Assert.Equal(0, pending.Count);
        }

        [Fact]
        public void Set_ManyDevices_CountsEachOnce()
        {
            // Arrange
            var pending = new PendingCommands();

            // Act
            for (var index = 0; index < 1000; index++)
            {
                pending.Set($"AA:BB:CC:DD:{index / 256:X2}:{index % 256:X2}", true);
            }

            // Assert
            Assert.Equal(1000, pending.Count);
        }

        [Fact]
        public void Set_ManyThreadsSameDevice_LeavesOneCommand()
        {
            // Arrange
            var pending = new PendingCommands();

            // Act
            Parallel.For(0, 5000, index => pending.Set(MacA, index % 2 == 0));

            // Assert
            Assert.Equal(1, pending.Count);
        }

        [Fact]
        public void TryTake_ManyThreadsOneCommand_OnlyOneThreadGetsIt()
        {
            // Arrange
            var pending = new PendingCommands();
            pending.Set(MacA, true);
            int got = 0;

            // Act
            Parallel.For(0, 5000, attempt =>
            {
                if (pending.TryTake(MacA, out _))
                {
                    Interlocked.Increment(ref got);
                }
            });

            // Assert
            Assert.Equal(1, got);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
