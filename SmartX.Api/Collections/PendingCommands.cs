//ST10445500 - PROG7312 - SmartX POE
//PendingCommands

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so a command sent to a valve waits until the valve comes to ask for it.

namespace SmartX.Api.Collections
{
    //keeps the newest command waiting for each device in a dictionary keyed on its mac address
    //setting, taking and checking one entry is O(1), and every member takes the one lock so requests and devices can share it
    public class PendingCommands
    {
        private readonly Dictionary<string, bool> _waiting = new();
        private readonly object _lock = new();

        //..............................................................................//

        //retrieves how many devices have a command waiting
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _waiting.Count;
                }
            }
        }

        //..............................................................................//

        //puts a command in for a device, replacing one that was still waiting
        public void Set(string macAddress, bool open)
        {
            lock (_lock)
            {
                // A valve only cares where it was last told to go, so the older command is dropped.
                _waiting[Key(macAddress)] = open;
            }
        }

        //..............................................................................//

        //hands over the command waiting for a device and removes it, returning false if there is none
        public bool TryTake(string macAddress, out bool open)
        {
            lock (_lock)
            {
                // Remove says whether it found the entry, so there is no separate check to race with.
                return _waiting.Remove(Key(macAddress), out open);
            }
        }

        //..............................................................................//

        //puts a mac address in the one form the dictionary is keyed on
        private static string Key(string macAddress)
        {
            return macAddress.ToUpperInvariant();
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
