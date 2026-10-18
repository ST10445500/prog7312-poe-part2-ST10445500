using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DeviceProfile

//.....................................o0oSTART OF FILEo0o........................................//

// A profile is the way one simulated device misbehaves, so a run is not all clean data.

namespace SmartX.Simulator
{
    //the ways a device can behave over time
    public enum DeviceProfile
    {
        //sits in its normal band the whole run
        Healthy,

        //a moisture sensor that dries out below the gateway's limit and is then watered
        Thirsty,

        //a power meter that surges past the gateway's wattage limit and settles
        Surging,

        //a radio that fades below the weak signal limit and comes back
        WeakRadio,

        //a radio that gets bursts of packet loss and a high duty cycle
        Flaky,

        //a valve that reports a fault code for a spell and then clears it
        FaultyValve,

        //a device that goes quiet for half a minute on its own
        Intermittent
    }

    //..............................................................................//

    //hands each device in the fleet its profile, so the same fleet always misbehaves the same way
    public static class DeviceProfiles
    {
        // Mostly healthy on purpose, or the dashboard is all red and nothing stands out.
        private static readonly DeviceProfile[] Moisture =
        [
            DeviceProfile.Healthy, DeviceProfile.Thirsty, DeviceProfile.Healthy, DeviceProfile.Healthy,
            DeviceProfile.WeakRadio, DeviceProfile.Healthy, DeviceProfile.Thirsty, DeviceProfile.Flaky,
            DeviceProfile.Healthy, DeviceProfile.Intermittent
        ];

        private static readonly DeviceProfile[] Power =
        [
            DeviceProfile.Healthy, DeviceProfile.Surging, DeviceProfile.Healthy, DeviceProfile.Flaky,
            DeviceProfile.Healthy, DeviceProfile.Surging, DeviceProfile.Healthy, DeviceProfile.WeakRadio,
            DeviceProfile.Intermittent, DeviceProfile.Healthy
        ];

        private static readonly DeviceProfile[] Valve =
        [
            DeviceProfile.Healthy, DeviceProfile.FaultyValve, DeviceProfile.Healthy, DeviceProfile.Intermittent,
            DeviceProfile.Healthy, DeviceProfile.FaultyValve, DeviceProfile.WeakRadio, DeviceProfile.Healthy
        ];

        //..............................................................................//

        //retrieves the profile for the nth reporting device of a category
        public static DeviceProfile For(SensorCategory category, int position)
        {
            var list = category switch
            {
                SensorCategory.Environmental => Moisture,
                SensorCategory.PowerConsumption => Power,
                _ => Valve
            };

            return list[position % list.Length];
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
