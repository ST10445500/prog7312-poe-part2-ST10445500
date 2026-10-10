//ST10445500 - PROG7312 - SmartX POE
//HistoryLog

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so a device history stays in time order however the readings arrive.

namespace SmartX.Api.Collections
{
    //keeps a time ordered history for each device, keyed on the device and then on the moment
    //a SortedDictionary is a red black tree, so an insert is O(log n) wherever in time it lands
    //a range walks the entries in order, which is O(n) because the built-in tree cannot jump to a start point, and n is held down by the cap
    //every member takes the one lock, and each device keeps at most the capacity, dropping the oldest
    public class HistoryLog<T>
    {
        private readonly Dictionary<string, SortedDictionary<DateTime, T>> _devices = new();
        private readonly int _capacity;
        private readonly object _lock = new();

        //..............................................................................//

        public HistoryLog(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "A history log needs room for at least one entry per device.");
            }

            _capacity = capacity;
        }

        //..............................................................................//

        //retrieves how many entries each device can keep
        public int Capacity => _capacity;

        //..............................................................................//

        //retrieves how many entries a device has, zero if it has none
        public int Count(string device)
        {
            lock (_lock)
            {
                return _devices.TryGetValue(device, out var entries) ? entries.Count : 0;
            }
        }

        //..............................................................................//

        //adds an entry at the moment it was recorded and returns the moment it was filed under
        public DateTime Add(string device, DateTime recordedAt, T entry)
        {
            lock (_lock)
            {
                if (!_devices.TryGetValue(device, out var entries))
                {
                    entries = new SortedDictionary<DateTime, T>();
                    _devices[device] = entries;
                }

                // Two readings can carry the same timestamp, and a plain indexer would overwrite the first.
                // Moving the later one on by a tick keeps both and their order.
                while (entries.ContainsKey(recordedAt))
                {
                    recordedAt = recordedAt.AddTicks(1);
                }

                entries.Add(recordedAt, entry);

                // The oldest key is the first one in order, even if the newest insert was out of order.
                while (entries.Count > _capacity)
                {
                    entries.Remove(entries.Keys.First());
                }

                return recordedAt;
            }
        }

        //..............................................................................//

        //retrieves a copy of the entries between two moments, oldest first, both ends included
        public List<T> GetRange(string device, DateTime from, DateTime to)
        {
            if (from > to)
            {
                throw new ArgumentException("The start of the range cannot be after the end.", nameof(from));
            }

            lock (_lock)
            {
                if (!_devices.TryGetValue(device, out var entries))
                {
                    return new List<T>();
                }

                // The tree is already in order, so there is no sort here.
                return entries
                    .Where(entry => entry.Key >= from && entry.Key <= to)
                    .Select(entry => entry.Value)
                    .ToList();
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
