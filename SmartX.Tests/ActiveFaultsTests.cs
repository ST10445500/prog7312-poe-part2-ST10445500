using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//ActiveFaultsTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that a known fault is only reported once, can be re-armed, stays per device and holds up under many threads
    public class ActiveFaultsTests
    {
        private const string MacA = "AA:BB:CC:DD:EE:01";
        private const string MacB = "AA:BB:CC:DD:EE:02";

        [Fact]
        public void TryRaise_SecondTime_ReturnsFalseAndCountsSuppressed()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.TryRaise(MacA, "E01");

            // Act
            var raised = faults.TryRaise(MacA, "E01");

            // Assert
            Assert.False(raised);
            Assert.Equal(1, faults.ActiveErrorCount);
            Assert.Equal(1, faults.SuppressedCount);
        }

        [Fact]
        public void TryRaise_ManyThreadsSameKey_OnlyOneReturnsTrue()
        {
            // Arrange
            var faults = new ActiveFaults();
            int trues = 0;

            // Act
            Parallel.For(0, 5000, _ =>
            {
                if (faults.TryRaise(MacA, "E01"))
                {
                    Interlocked.Increment(ref trues);
                }
            });

            // Assert
            Assert.Equal(1, trues);
            Assert.Equal(1, faults.ActiveErrorCount);
            Assert.Equal(4999, faults.SuppressedCount);
        }

        [Fact]
        public void MarkDisconnected_SecondTime_ReturnsFalseAndCountsSuppressed()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.MarkDisconnected(MacA);

            // Act
            var marked = faults.MarkDisconnected(MacA);

            // Assert
            Assert.False(marked);
            Assert.Equal(1, faults.SuppressedDisconnectCount);
            Assert.Equal(0, faults.SuppressedCount);
        }

        [Fact]
        public void Report_NoFaultInBetween_RearmsTheFault()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.Report(MacA, "StuckValve");

            // Act
            var cleared = faults.Report(MacA, null);
            var again = faults.Report(MacA, "StuckValve");

            // Assert
            Assert.False(cleared);
            Assert.True(again);
            Assert.Equal(0, faults.SuppressedCount);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
