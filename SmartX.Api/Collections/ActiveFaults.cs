//ST10445500 - PROG7312 - SmartX POE
//ActiveFaults

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so a fault that is already known is not reported again on every reading.

namespace SmartX.Api.Collections
{
    //keeps the devices that have gone quiet and the faults that are currently active, each in a hash set
    //adding, removing and checking one entry is O(1), and every member takes the one lock so requests and the processor can share it
    public class ActiveFaults
    {
        private readonly HashSet<string> _disconnected = new();
        private readonly HashSet<(string Mac, string Code)> _errors = new();
        private readonly object _lock = new();

        private long _suppressed;

        //..............................................................................//

        //retrieves how many faults are active right now
        public int ActiveErrorCount
        {
            get
            {
                lock (_lock)
                {
                    return _errors.Count;
                }
            }
        }

        //retrieves how many repeats of an active fault were turned away
        public long SuppressedCount
        {
            get
            {
                lock (_lock)
                {
                    return _suppressed;
                }
            }
        }

        //..............................................................................//

        //marks a fault active, returning false and counting a suppressed repeat if it already was
        public bool TryRaise(string macAddress, string code)
        {
            var key = (Key(macAddress), code);

            lock (_lock)
            {
                // Add already says whether the entry was new, so there is no separate check to race with.
                if (_errors.Add(key))
                {
                    return true;
                }

                _suppressed++;
                return false;
            }
        }

        //..............................................................................//

        //records what a device is reporting right now, returning true only if that fault was not already active
        //anything else the device had active is cleared first, and a null code means it is reporting no fault at all
        public bool Report(string macAddress, string? code)
        {
            var mac = Key(macAddress);

            // One lock hold for the clear and the raise, so two posts from one device cannot slip between them.
            lock (_lock)
            {
                _errors.RemoveWhere(error => error.Mac == mac && error.Code != code);

                if (code == null)
                {
                    return false;
                }

                if (_errors.Add((mac, code)))
                {
                    return true;
                }

                _suppressed++;
                return false;
            }
        }

        //..............................................................................//

        //clears every active fault of a device except the one still happening, so a fault that stopped can be raised again
        //pass null to clear them all, returns how many were cleared
        public int ClearExcept(string macAddress, string? keepCode)
        {
            var mac = Key(macAddress);

            lock (_lock)
            {
                // The set is not indexed by device, so this walks every active fault. The set stays small.
                return _errors.RemoveWhere(error => error.Mac == mac && error.Code != keepCode);
            }
        }

        //..............................................................................//

        //marks a device as gone quiet, returning false if it was already marked
        public bool MarkDisconnected(string macAddress)
        {
            lock (_lock)
            {
                return _disconnected.Add(Key(macAddress));
            }
        }

        //..............................................................................//

        //marks a device as back, returning false if it was not marked quiet
        public bool MarkConnected(string macAddress)
        {
            lock (_lock)
            {
                return _disconnected.Remove(Key(macAddress));
            }
        }

        //..............................................................................//

        //retrieves a sorted copy of every active fault, so callers never walk the set while it changes
        public List<(string Mac, string Code)> GetErrors()
        {
            lock (_lock)
            {
                return _errors
                    .OrderBy(error => error.Mac, StringComparer.Ordinal)
                    .ThenBy(error => error.Code, StringComparer.Ordinal)
                    .ToList();
            }
        }

        //..............................................................................//

        //retrieves a sorted copy of the devices that have gone quiet
        public List<string> GetDisconnected()
        {
            lock (_lock)
            {
                return _disconnected
                    .OrderBy(mac => mac, StringComparer.Ordinal)
                    .ToList();
            }
        }

        //..............................................................................//

        //puts a mac address in the one form the sets are keyed on
        private static string Key(string macAddress)
        {
            return macAddress.ToUpperInvariant();
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
