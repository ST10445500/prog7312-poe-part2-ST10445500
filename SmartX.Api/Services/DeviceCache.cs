using SmartX.Api.Models.Stream;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DeviceCache

//.....................................o0oSTART OF FILEo0o........................................//

// The cache keeps the latest state of each device in memory instead of using a database.

namespace SmartX.Api.Services
{
    //keeps the latest state of every device the stream has seen
    //keyed on the mac address so a lookup is O(1), and every member takes the one lock
    public class DeviceCache
    {
        private readonly Dictionary<string, DeviceState> _devices = new();
        private readonly object _lock = new();

        //..............................................................................//

        //replaces what is known about a device with the packet that just came off the queue
        public void Update(StreamPacket packet, string name)
        {
            var key = packet.MacAddress.ToUpperInvariant();

            var state = new DeviceState
            {
                MacAddress = key,
                Name = name,
                Category = packet.Category,
                Reading = Describe(packet),
                State = packet.State,
                LastSeen = packet.RecordedAt,
                Radio = packet.Radio
            };

            lock (_lock)
            {
                _devices[key] = state;
            }
        }

        //..............................................................................//

        //retrieves a device by mac address, or null if the stream has not seen it
        public DeviceState? Find(string macAddress)
        {
            lock (_lock)
            {
                _devices.TryGetValue(macAddress.ToUpperInvariant(), out var state);
                return state;
            }
        }

        //..............................................................................//

        //retrieves a copy of every device, so callers never walk the dictionary while it changes
        public List<DeviceState> GetAll()
        {
            lock (_lock)
            {
                return _devices.Values
                    .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(device => device.MacAddress, StringComparer.Ordinal)
                    .ToList();
            }
        }

        //..............................................................................//

        //builds the reading with its unit on it
        private static string Describe(StreamPacket packet)
        {
            switch (packet.Category)
            {
                case SensorCategory.Environmental:
                    return $"{packet.Value:0.#}%";

                case SensorCategory.PowerConsumption:
                    return $"{packet.Value:0}W";

                default:
                    return packet.State == true ? "Open" : "Closed";
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
