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
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
