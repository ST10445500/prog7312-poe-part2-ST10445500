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
        public void Enqueue_FromManyThreads_KeepsEveryItem()
        {
            // Arrange
            var queue = new AlertQueue<int>();

            // Act
            Parallel.For(0, 5000, i => queue.Enqueue(i, i % 2 + 1));

            // Assert
            Assert.Equal(5000, queue.Count);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
