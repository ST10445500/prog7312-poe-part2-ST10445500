//ST10445500 - PROG7312 - SmartX POE
//CommandHistory

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written to hand the newest override back first, so undo walks backwards.

namespace SmartX.Api.Collections
{
    //keeps manual overrides with the newest on top
    //push and pop are O(1), and every member takes the one lock
    public class CommandHistory<T>
    {
        private readonly Stack<T> _items = new();
        private readonly object _lock = new();

        //..............................................................................//

        //retrieves how many overrides can still be undone
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

        //adds an override on top of the others
        public void Push(T item)
        {
            lock (_lock)
            {
                _items.Push(item);
            }
        }

        //..............................................................................//

        //retrieves a copy of every override, newest first, so the stack itself is never handed out
        //copying is O(n) and the lock is held only while it happens
        public List<T> GetAll()
        {
            lock (_lock)
            {
                // A stack enumerates from the top, which is the order undo takes them.
                return _items.ToList();
            }
        }

        //..............................................................................//

        //takes the newest override off, returning false if there is nothing to undo
        public bool TryPop(out T? item)
        {
            lock (_lock)
            {
                return _items.TryPop(out item);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
