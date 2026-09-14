using System.Collections;

//ST10445500 - PROG7312 - SmartX POE
//RingBuffer

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so the gateway can control how memory is used.

namespace SmartX.Api.Collections
{
    //keeps the most recent items in a fixed size array that wraps around
    //once it is full the oldest item is overwritten, so memory never grows
    public class RingBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _items;

        //where the next item will be written
        private int _nextSlot;

        //how many slots have been filled so far
        private int _count;

        //..............................................................................//

        public RingBuffer(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "A ring buffer needs room for at least one item.");
            }

            _items = new T[capacity];
        }

        //..............................................................................//

        //retrieves how many items the buffer can hold
        public int Capacity => _items.Length;

        //retrieves how many items are currently stored
        public int Count => _count;

        //checks whether the buffer has started overwriting old items
        public bool IsFull => _count == _items.Length;

        //..............................................................................//

        //adds an item and overwrites the oldest one once the buffer is full
        public void Add(T item)
        {
            _items[_nextSlot] = item;

            // Wrapping is what makes this fixed size.
            _nextSlot = (_nextSlot + 1) % _items.Length;

            if (_count < _items.Length)
            {
                _count++;
            }
        }

        //..............................................................................//

        //retrieves an item by position, counting from the oldest one still held
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                // After a wrap the oldest item is the one about to be overwritten.
                var oldest = IsFull ? _nextSlot : 0;
                return _items[(oldest + index) % _items.Length];
            }
        }

        //..............................................................................//

        //retrieves the newest item, or throws if nothing has been added yet
        public T Newest()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The ring buffer is empty.");
            }

            return this[_count - 1];
        }

        //..............................................................................//

        //retrieves everything in the buffer as a list, oldest first
        public List<T> ToList()
        {
            var items = new List<T>(_count);

            for (var i = 0; i < _count; i++)
            {
                items.Add(this[i]);
            }

            return items;
        }

        //..............................................................................//

        //removes everything from the buffer
        public void Clear()
        {
            Array.Clear(_items);
            _nextSlot = 0;
            _count = 0;
        }

        //..............................................................................//

        //walks the buffer from oldest to newest so it can be used in a foreach
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
            {
                yield return this[i];
            }
        }

        //walks the buffer for code that asks for the older non generic version
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
