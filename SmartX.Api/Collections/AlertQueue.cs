//ST10445500 - PROG7312 - SmartX POE
//AlertQueue

//.....................................o0oSTART OF FILEo0o........................................//

// This collection lets urgent alerts leave before the ordinary packets waiting behind them.

namespace SmartX.Api.Collections
{
    //keeps alerts ordered by severity, with the most severe first and equal severity in arrival order
    //enqueue and dequeue are O(log n), and every member takes the lock so requests and the processor can share it
    public class AlertQueue<T>
    {
        private readonly PriorityQueue<T, (int Rank, long Sequence)> _items = new();
        private readonly object _lock = new();

        //counts every alert ever added, so two alerts of the same severity can be told apart by arrival
        private long _sequence;

        //..............................................................................//

        //retrieves how many alerts are waiting
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

        //adds an alert, a higher severity number leaves the queue sooner
        public void Enqueue(T item, int severity)
        {
            lock (_lock)
            {
                // The built-in queue takes the smallest first, so the severity is flipped.
                // The sequence breaks ties because the built-in queue does not keep arrival order by itself.
                _items.Enqueue(item, (-severity, _sequence++));
            }
        }

        //..............................................................................//

        //takes the most severe alert off the queue, returning false if nothing is waiting
        public bool TryDequeue(out T? item)
        {
            lock (_lock)
            {
                return _items.TryDequeue(out item, out _);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
