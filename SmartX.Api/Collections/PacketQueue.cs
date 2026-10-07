//ST10445500 - PROG7312 - SmartX POE
//PacketQueue

//.....................................o0oSTART OF FILEo0o........................................//

// This collection holds incoming packets in arrival order so the processor can take them one at a time.

namespace SmartX.Api.Collections
{
    //keeps packets waiting in first in first out order behind one lock
    //enqueue and dequeue are O(1), and every member takes the lock so requests and the processor can share it
    public class PacketQueue<T>
    {
        private readonly Queue<T> _items = new();
        private readonly object _lock = new();

        //..............................................................................//

        //retrieves how many packets are waiting
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _items.Count;
                }
            }
        }

        //..............................................................................//

        //adds a packet to the back of the queue
        public void Enqueue(T item)
        {
            lock (_lock)
            {
                _items.Enqueue(item);
            }
        }

        //..............................................................................//

        //takes the oldest packet off the queue, returning false if nothing is waiting
        public bool TryDequeue(out T? item)
        {
            lock (_lock)
            {
                return _items.TryDequeue(out item);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
