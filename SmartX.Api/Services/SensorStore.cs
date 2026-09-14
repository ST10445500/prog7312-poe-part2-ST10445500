using System.Collections.Concurrent;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//SensorStore

//.....................................o0oSTART OF FILEo0o........................................//

// The store keeps gateway data in memory instead of using a database.

namespace SmartX.Api.Services
{
    //keeps every registered sensor in memory
    //keyed on the mac address so a lookup does not have to scan a list
    public class SensorStore
    {
        private readonly ConcurrentDictionary<string, SensorRegistration> _sensors = new();

        //..............................................................................//

        //registers a sensor and returns false if the mac address is already taken
        public bool TryRegister(SensorRegistration sensor)
        {
            // Mac addresses are not case sensitive, so one device cannot register twice.
            var key = sensor.MacAddress.ToUpperInvariant();
            sensor.MacAddress = key;

            return _sensors.TryAdd(key, sensor);
        }

        //..............................................................................//

        //retrieves every sensor registered with the gateway, in deployment order
        public List<SensorRegistration> GetAll()
        {
            // A ConcurrentDictionary returns values in no order, and callers should not have to sort.
            return _sensors.Values
                .OrderBy(sensor => sensor.Location.Zone, StringComparer.OrdinalIgnoreCase)
                .ThenBy(sensor => sensor.Location.Room, StringComparer.OrdinalIgnoreCase)
                .ThenBy(sensor => sensor.Location.NodeId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(sensor => sensor.MacAddress, StringComparer.Ordinal)
                .ToList();
        }

        //..............................................................................//

        //retrieves a sensor by mac address, or null if it is not registered
        public SensorRegistration? Find(string macAddress)
        {
            _sensors.TryGetValue(macAddress.ToUpperInvariant(), out var sensor);
            return sensor;
        }

        //..............................................................................//

        //records where a sensor sits, worked out from the deployment tree
        public void SetLocation(string macAddress, string zone, string room)
        {
            var sensor = Find(macAddress);

            if (sensor == null)
            {
                return;
            }

            sensor.Location.Zone = zone;
            sensor.Location.Room = room;
        }

        //..............................................................................//

        //checks whether a mac address belongs to a registered sensor
        public bool IsRegistered(string macAddress)
        {
            return Find(macAddress) != null;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
