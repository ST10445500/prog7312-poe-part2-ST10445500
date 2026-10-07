using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//PacketQueueTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that packets come back out in the order they went in
    public class PacketQueueTests
    {
        [Fact]
        public void TryDequeue_EmptyQueue_ReturnsFalse()
        {
            // Arrange
            var queue = new PacketQueue<int>();

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
            var queue = new PacketQueue<int>();
            queue.Enqueue(7);

            // Act
            var found = queue.TryDequeue(out var item);

            // Assert
            Assert.True(found);
            Assert.Equal(7, item);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void TryDequeue_ManyItems_ComesBackInArrivalOrder()
        {
            // Arrange
            var queue = new PacketQueue<int>();

            for (var i = 0; i < 1000; i++)
            {
                queue.Enqueue(i);
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
            var queue = new PacketQueue<int>();

            // Act
            Parallel.For(0, 5000, i => queue.Enqueue(i));

            // Assert
            Assert.Equal(5000, queue.Count);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
