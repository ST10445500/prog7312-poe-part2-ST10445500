using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DemoFleet

//.....................................o0oSTART OF FILEo0o........................................//

// The service keeps business rules and validation away from the controller.

namespace SmartX.Api.Services
{
    //registers a small demo facility so the gateway has something to show on a first run
    public static class DemoFleet
    {
        //..............................................................................//

        //registers every demo sensor the gateway does not already know about
        public static void Seed(SensorStore sensors)
        {
            foreach (var sensor in Build())
            {
                // TryRegister leaves an existing mac alone.
                sensors.TryRegister(sensor);
            }
        }

        //..............................................................................//

        //builds the demo facility, covering all three categories across a few rooms
        private static List<SensorRegistration> Build()
        {
            var registeredAt = DateTime.UtcNow;

            return
            [
                Sensor("AA:BB:CC:DD:EE:01", "Greenhouse A", "Bay 1", "soil-a1", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:02", "Greenhouse A", "Bay 1", "valve-a1", SensorCategory.Actuator, registeredAt),
                Sensor("AA:BB:CC:DD:EE:03", "Greenhouse A", "Bay 2", "soil-a2", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:04", "Greenhouse A", "Bay 2", "meter-a2", SensorCategory.PowerConsumption, registeredAt),
                Sensor("AA:BB:CC:DD:EE:05", "Greenhouse B", "Bay 1", "soil-b1", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:06", "Greenhouse B", "Bay 1", "valve-b1", SensorCategory.Actuator, registeredAt),
                Sensor("AA:BB:CC:DD:EE:07", "Utility", "Main", "meter-main", SensorCategory.PowerConsumption, registeredAt),
                Sensor("AA:BB:CC:DD:EE:08", "Utility", "Main", "meter-pump", SensorCategory.PowerConsumption, registeredAt),

                // Registered but never powered up. The simulator skips it.
                Sensor("AA:BB:CC:DD:EE:09", "Utility", "Store Room", "spare-valve", SensorCategory.Actuator, registeredAt)
            ];
        }

        //..............................................................................//

        //builds one demo registration in the shape a device would have been registered with
        private static SensorRegistration Sensor(string macAddress, string zone, string room, string nodeId, SensorCategory category, DateTime registeredAt)
        {
            return new SensorRegistration
            {
                MacAddress = macAddress,
                Category = category,
                RegisteredAt = registeredAt,
                Location = new DeploymentLocation
                {
                    Zone = zone,
                    Room = room,
                    NodeId = nodeId
                }
            };
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
