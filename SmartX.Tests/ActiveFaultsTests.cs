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
        public void TryRaise_FirstTime_ReturnsTrueAndCountsOne()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            var raised = faults.TryRaise(MacA, "E01");

            // Assert
            Assert.True(raised);
            Assert.Equal(1, faults.ActiveErrorCount);
        }

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
        public void TryRaise_SeveralRepeats_CountsEachSuppressed()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.TryRaise(MacA, "E01");

            // Act
            for (int i = 0; i < 5; i++)
            {
                faults.TryRaise(MacA, "E01");
            }

            // Assert
            Assert.Equal(1, faults.ActiveErrorCount);
            Assert.Equal(5, faults.SuppressedCount);
        }

        [Fact]
        public void ClearExcept_NullKeep_ClearsAllForDeviceAndRearms()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.TryRaise(MacA, "E01");
            faults.TryRaise(MacA, "E02");

            // Act
            var cleared = faults.ClearExcept(MacA, null);
            var raisedAgain = faults.TryRaise(MacA, "E01");

            // Assert
            Assert.Equal(2, cleared);
            Assert.True(raisedAgain);
            Assert.Equal(1, faults.ActiveErrorCount);
        }

        [Fact]
        public void ClearExcept_KeepOneCode_LeavesOnlyThatOne()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.TryRaise(MacA, "E01");
            faults.TryRaise(MacA, "E02");
            faults.TryRaise(MacA, "E03");

            // Act
            var cleared = faults.ClearExcept(MacA, "E02");

            // Assert
            Assert.Equal(2, cleared);
            Assert.Equal(1, faults.ActiveErrorCount);
            Assert.False(faults.TryRaise(MacA, "E02"));
            Assert.True(faults.TryRaise(MacA, "E01"));
        }

        [Fact]
        public void ClearExcept_UnknownMac_ReturnsZero()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.TryRaise(MacA, "E01");

            // Act
            var cleared = faults.ClearExcept(MacB, null);

            // Assert
            Assert.Equal(0, cleared);
            Assert.Equal(1, faults.ActiveErrorCount);
        }

        [Fact]
        public void TryRaise_SameCodeOnDifferentMacs_AreIndependent()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            var first = faults.TryRaise(MacA, "E01");
            var second = faults.TryRaise(MacB, "E01");

            // Assert
            Assert.True(first);
            Assert.True(second);
            Assert.Equal(2, faults.ActiveErrorCount);
            Assert.Equal(0, faults.SuppressedCount);
        }

        [Fact]
        public void TryRaise_DifferentCodesOnSameMac_AreIndependent()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            var first = faults.TryRaise(MacA, "E01");
            var second = faults.TryRaise(MacA, "E02");

            // Assert
            Assert.True(first);
            Assert.True(second);
            Assert.Equal(2, faults.ActiveErrorCount);
        }

        [Fact]
        public void TryRaise_MacInOtherCase_IsTheSameDevice()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.TryRaise("aa:bb:cc:dd:ee:01", "E01");

            // Act
            var raised = faults.TryRaise("AA:BB:CC:DD:EE:01", "E01");

            // Assert
            Assert.False(raised);
            Assert.Equal(1, faults.ActiveErrorCount);
        }

        [Fact]
        public void MarkDisconnected_FirstThenSecond_ReturnsTrueThenFalse()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            var first = faults.MarkDisconnected(MacA);
            var second = faults.MarkDisconnected(MacA);

            // Assert
            Assert.True(first);
            Assert.False(second);
        }

        [Fact]
        public void MarkConnected_DeviceNotMarked_ReturnsFalse()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            var result = faults.MarkConnected(MacA);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void MarkConnected_AfterMarkDisconnected_ReturnsTrueAndRearms()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.MarkDisconnected(MacA);

            // Act
            var back = faults.MarkConnected(MacA);
            var goneAgain = faults.MarkDisconnected(MacA);

            // Assert
            Assert.True(back);
            Assert.True(goneAgain);
        }

        [Fact]
        public void GetDisconnected_SeveralDevices_ReturnsSortedList()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.MarkDisconnected("AA:BB:CC:DD:EE:03");
            faults.MarkDisconnected("AA:BB:CC:DD:EE:01");
            faults.MarkDisconnected("AA:BB:CC:DD:EE:02");

            // Act
            var list = faults.GetDisconnected();

            // Assert
            Assert.Equal(new[] { "AA:BB:CC:DD:EE:01", "AA:BB:CC:DD:EE:02", "AA:BB:CC:DD:EE:03" }, list);
        }

        [Fact]
        public void GetDisconnected_ChangingTheList_DoesNotChangeTheSet()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.MarkDisconnected(MacA);
            var list = faults.GetDisconnected();

            // Act
            list.Clear();

            // Assert
            Assert.Single(faults.GetDisconnected());
        }

        [Fact]
        public void GetErrors_EmptySet_ReturnsEmptyList()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            var list = faults.GetErrors();

            // Assert
            Assert.Empty(list);
        }

        [Fact]
        public void GetErrors_SeveralFaults_ReturnsSortedAndChangingTheListLeavesTheSet()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.TryRaise(MacB, "E01");
            faults.TryRaise(MacA, "E02");
            faults.TryRaise(MacA, "E01");

            // Act
            var list = faults.GetErrors();
            var expected = new[] { (MacA, "E01"), (MacA, "E02"), (MacB, "E01") };
            var sorted = list.ToArray();
            list.Clear();

            // Assert
            Assert.Equal(expected, sorted);
            Assert.Equal(3, faults.GetErrors().Count);
            Assert.Equal(3, faults.ActiveErrorCount);
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
        public void TryRaise_ManyThreadsDistinctKeys_KeepsAll()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            Parallel.For(0, 5000, i =>
            {
                faults.TryRaise(MacA, "E" + i);
            });

            // Assert
            Assert.Equal(5000, faults.ActiveErrorCount);
            Assert.Equal(0, faults.SuppressedCount);
        }

        [Fact]
        public void MarkDisconnected_ManyThreadsSameMac_OnlyOneReturnsTrue()
        {
            // Arrange
            var faults = new ActiveFaults();
            int trues = 0;

            // Act
            Parallel.For(0, 5000, _ =>
            {
                if (faults.MarkDisconnected(MacA))
                {
                    Interlocked.Increment(ref trues);
                }
            });

            // Assert
            Assert.Equal(1, trues);
            Assert.Single(faults.GetDisconnected());
        }

        [Fact]
        public void Report_NewFaultThenRepeat_TrueThenFalseAndCounted()
        {
            // Arrange
            var faults = new ActiveFaults();

            // Act
            var first = faults.Report(MacA, "StuckValve");
            var second = faults.Report(MacA, "StuckValve");

            // Assert
            Assert.True(first);
            Assert.False(second);
            Assert.Equal(1, faults.ActiveErrorCount);
            Assert.Equal(1, faults.SuppressedCount);
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

        [Fact]
        public void Report_DifferentFaultOnSameDevice_ClearsTheOldOne()
        {
            // Arrange
            var faults = new ActiveFaults();
            faults.Report(MacA, "StuckValve");

            // Act
            var raised = faults.Report(MacA, "WeakSignal");

            // Assert
            Assert.True(raised);
            Assert.Equal(1, faults.ActiveErrorCount);
        }

        [Fact]
        public void Report_ManyThreadsSameFault_OnlyOneReturnsTrue()
        {
            // Arrange
            var faults = new ActiveFaults();
            int trues = 0;

            // Act
            Parallel.For(0, 5000, _ =>
            {
                if (faults.Report(MacA, "StuckValve"))
                {
                    Interlocked.Increment(ref trues);
                }
            });

            // Assert
            Assert.Equal(1, trues);
            Assert.Equal(4999, faults.SuppressedCount);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
