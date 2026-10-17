using SmartX.Api.Models.Stream;

//ST10445500 - PROG7312 - SmartX POE
//TransitionModel

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so the engine can count which event follows which and rank what is likely next.

namespace SmartX.Api.Collections
{
    //counts how often one event is followed by another in a dictionary of dictionaries keyed on event names
    //recording is O(1), ranking what follows an event is O(k log k) for the k events seen after it, and every member takes the one lock
    public class TransitionModel
    {
        private readonly Dictionary<string, Dictionary<string, int>> _counts = new();
        private readonly object _lock = new();
        private string? _last;

        //..............................................................................//

        //retrieves the event seen most recently, or null before any
        public string? Last
        {
            get
            {
                lock (_lock)
                {
                    return _last;
                }
            }
        }

        //..............................................................................//

        //adds one to the count of an event following another
        public void Record(string from, string to)
        {
            Check(from);
            Check(to);

            lock (_lock)
            {
                Add(from, to);
            }
        }

        //..............................................................................//

        //notes the next event in order, counting it as following the one before
        public void Observe(string next)
        {
            Check(next);

            lock (_lock)
            {
                // Reading the last event and setting it in one hold keeps two threads from pairing the same one twice.
                if (_last != null)
                {
                    Add(_last, next);
                }

                _last = next;
            }
        }

        //..............................................................................//

        //retrieves up to take events that followed this one, most often first, with ties in name order
        public List<Prediction> GetNext(string from, int take)
        {
            lock (_lock)
            {
                if (take <= 0 || string.IsNullOrWhiteSpace(from) || !_counts.TryGetValue(from, out var followers))
                {
                    return new List<Prediction>();
                }

                var total = followers.Values.Sum();

                // Ordinal so the tie order is the same on every machine.
                return followers
                    .OrderByDescending(pair => pair.Value)
                    .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                    .Take(take)
                    .Select(pair => new Prediction
                    {
                        Event = pair.Key,
                        Count = pair.Value,
                        Confidence = (double)pair.Value / total
                    })
                    .ToList();
            }
        }

        //..............................................................................//

        //retrieves how many times anything has followed this event
        public int Total(string from)
        {
            lock (_lock)
            {
                return !string.IsNullOrWhiteSpace(from) && _counts.TryGetValue(from, out var followers)
                    ? followers.Values.Sum()
                    : 0;
            }
        }

        //..............................................................................//

        //adds one to a pair, the caller already holds the lock
        private void Add(string from, string to)
        {
            if (!_counts.TryGetValue(from, out var followers))
            {
                followers = new Dictionary<string, int>();
                _counts[from] = followers;
            }

            followers[to] = followers.GetValueOrDefault(to) + 1;
        }

        //..............................................................................//

        //refuses an event name that is empty
        private static void Check(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("An event name cannot be empty.", nameof(name));
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
