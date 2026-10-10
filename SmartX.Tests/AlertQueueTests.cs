using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//AlertQueueTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that alerts leave by severity first and by arrival order within the same severity
    public class AlertQueueTests
    {
        [Fact]
        public void TryDequeue_EmptyQueue_ReturnsFalse()
        {
            // Arrange
            var queue = new AlertQueue<int>();

            // Act
            var found = queue.TryDequeue(out _);

            // Assert
            Assert.False(found);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void TryDequeue_OneItem_ReturnsItAndEmpties()
        {
            // Arrange
            var queue = new AlertQueue<int>();
            queue.Enqueue(7, 1);

            // Act
            var found = queue.TryDequeue(out var item);

            // Assert
            Assert.True(found);
            Assert.Equal(7, item);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void TryDequeue_MixedSeverity_HigherSeverityLeavesFirst()
        {
            // Arrange
            var queue = new AlertQueue<string>();
            queue.Enqueue("warning", 1);
            queue.Enqueue("critical", 2);

            // Act
            queue.TryDequeue(out var first);
            queue.TryDequeue(out var second);

            // Assert
            Assert.Equal("critical", first);
            Assert.Equal("warning", second);
        }

        [Fact]
        public void TryDequeue_EqualSeverity_ComesBackInArrivalOrder()
        {
            // Arrange
            var queue = new AlertQueue<int>();

            for (var i = 0; i < 1000; i++)
            {
                queue.Enqueue(i, 2);
            }

            // Act
            var order = new List<int>();

            while (queue.TryDequeue(out var item))
            {
                order.Add(item);
            }

            // Assert
            Assert.Equal(Enumerable.Range(0, 1000), order);
        }

        [Fact]
        public void TryDequeue_AlertAddedBehindOrdinaryPackets_IsHandledFirst()
        {
            // Arrange
            var packets = new PacketQueue<int>();
            var alerts = new AlertQueue<int>();

            for (var i = 0; i < 100; i++)
            {
                packets.Enqueue(i);
            }

            alerts.Enqueue(999, 2);

            // Act
            // Alerts are taken first, the same way the processor does it.
            var handled = new List<int>();

            while (true)
            {
                if (alerts.TryDequeue(out var alert))
                {
                    handled.Add(alert);
                }
                else if (packets.TryDequeue(out var packet))
                {
                    handled.Add(packet);
                }
                else
                {
                    break;
                }
            }

            // Assert
            Assert.Equal(101, handled.Count);
            Assert.Equal(999, handled[0]);
            Assert.Equal(Enumerable.Range(0, 100), handled.Skip(1));
        }

        [Fact]
        public void Enqueue_FromManyThreads_KeepsEveryItem()
        {
            // Arrange
            var queue = new AlertQueue<int>();

            // Act
            Parallel.For(0, 5000, i => queue.Enqueue(i, i % 2 + 1));

            // Assert
            Assert.Equal(5000, queue.Count);
        }

        [Fact]
        public void TryDequeue_LargeMixedLoad_ComesBackBySeverityThenArrival()
        {
            // Arrange
            var queue = new AlertQueue<(int Id, int Severity)>();

            for (var i = 0; i < 3000; i++)
            {
                queue.Enqueue((i, i % 3), i % 3);
            }

            // Act
            var order = new List<(int Id, int Severity)>();

            while (queue.TryDequeue(out var item))
            {
                order.Add(item);
            }

            // Assert
            var expected = Enumerable.Range(0, 3000)
                .Select(i => (Id: i, Severity: i % 3))
                .OrderByDescending(x => x.Severity)
                .ThenBy(x => x.Id)
                .ToList();

            Assert.Equal(expected, order);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
