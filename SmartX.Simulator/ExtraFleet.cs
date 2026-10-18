using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//ExtraFleet

//.....................................o0oSTART OF FILEo0o........................................//

// The extra fleet is the devices the simulator registers itself, on top of the gateway's demo nine.

namespace SmartX.Simulator
{
    //builds the extra rooms so the table, search and filters have a real number of devices to work on
    public static class ExtraFleet
    {
        //..............................................................................//

        //builds every extra device, in the order the generators will be seeded
        public static List<SensorRegistration> Build()
        {
            var registeredAt = DateTime.UtcNow;

            return
            [
                Sensor("AA:BB:CC:DD:EE:10", "Greenhouse C", "Bay 1", "soil-c1", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:11", "Greenhouse C", "Bay 1", "soil-c2", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:12", "Greenhouse C", "Bay 1", "valve-c1", SensorCategory.Actuator, registeredAt),
                Sensor("AA:BB:CC:DD:EE:13", "Greenhouse C", "Bay 2", "soil-c3", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:14", "Greenhouse C", "Bay 2", "soil-c4", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:15", "Greenhouse C", "Bay 2", "valve-c2", SensorCategory.Actuator, registeredAt),
                Sensor("AA:BB:CC:DD:EE:16", "Greenhouse C", "Bay 2", "meter-c2", SensorCategory.PowerConsumption, registeredAt),

                Sensor("AA:BB:CC:DD:EE:17", "Cold Store", "Chiller", "meter-chiller", SensorCategory.PowerConsumption, registeredAt),
                Sensor("AA:BB:CC:DD:EE:18", "Cold Store", "Chiller", "meter-fans", SensorCategory.PowerConsumption, registeredAt),
                Sensor("AA:BB:CC:DD:EE:19", "Cold Store", "Dock", "valve-dock", SensorCategory.Actuator, registeredAt),

                Sensor("AA:BB:CC:DD:EE:20", "Workshop", "Bench", "meter-bench", SensorCategory.PowerConsumption, registeredAt),
                Sensor("AA:BB:CC:DD:EE:21", "Workshop", "Bench", "meter-welder", SensorCategory.PowerConsumption, registeredAt),
                Sensor("AA:BB:CC:DD:EE:22", "Workshop", "Compressor", "meter-comp", SensorCategory.PowerConsumption, registeredAt),

                Sensor("AA:BB:CC:DD:EE:23", "Pump House", "Main", "valve-p1", SensorCategory.Actuator, registeredAt),
                Sensor("AA:BB:CC:DD:EE:24", "Pump House", "Main", "valve-p2", SensorCategory.Actuator, registeredAt),
                Sensor("AA:BB:CC:DD:EE:25", "Pump House", "Main", "valve-p3", SensorCategory.Actuator, registeredAt),
                Sensor("AA:BB:CC:DD:EE:26", "Pump House", "Main", "meter-p2", SensorCategory.PowerConsumption, registeredAt),
                Sensor("AA:BB:CC:DD:EE:27", "Pump House", "Yard", "soil-p1", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:28", "Pump House", "Yard", "soil-p2", SensorCategory.Environmental, registeredAt),
                Sensor("AA:BB:CC:DD:EE:29", "Pump House", "Yard", "soil-p3", SensorCategory.Environmental, registeredAt)
            ];
        }

        //..............................................................................//

        //builds one registration in the shape the gateway takes
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
