using SmartX.Api.Models.Telemetry;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//TelemetryHealth

//.....................................o0oSTART OF FILEo0o........................................//

// The service keeps business rules and validation away from the controller.

namespace SmartX.Api.Services
{
    //splits recent readings into equal windows and says what happened in each
    //an empty window is a disconnect, a sharp change is a spike
    public class TelemetryHealth
    {
        //how many windows are worked out when the caller does not ask for a number
        public const int DefaultWindowCount = 24;

        //how many seconds each window covers when the caller does not ask
        public const int DefaultWindowSeconds = 10;

        //the most windows the gateway will work out in one call
        public const int MaxWindowCount = 240;

        //the longest one window may cover, which is an hour
        public const int MaxWindowSeconds = 3600;

        //how many quiet windows before a sensor counts as off the air
        public const int SilentAfterWindows = 2;

        // Starting values picked for the demo. Real use would learn them per device.

        //how many watts a meter has to jump to count as a spike
        public const int PowerSpikeWatts = 50;

        //how far moisture has to move between two readings before it counts as a spike
        public const float MoistureSpikePercent = 15f;

        //how many state changes in a window count as flapping
        public const int ActuatorFlapChanges = 3;

        private readonly TelemetryStore _telemetry;

        //..............................................................................//

        public TelemetryHealth(TelemetryStore telemetry)
        {
            _telemetry = telemetry;
        }

        //..............................................................................//

        //retrieves how a sensor has been behaving over the last stretch of windows, oldest first
        public SensorHealth Build(SensorRegistration sensor, int windowCount, int windowSeconds, DateTime now)
        {
            windowCount = Math.Clamp(windowCount, 1, MaxWindowCount);
            windowSeconds = Math.Clamp(windowSeconds, 1, MaxWindowSeconds);

            var health = new SensorHealth
            {
                MacAddress = sensor.MacAddress,
                Category = sensor.Category,
                WindowSeconds = windowSeconds,
                GeneratedAt = now
            };

            var start = FirstWindowStart(now, windowCount, windowSeconds);

            switch (sensor.Category)
            {
                case SensorCategory.Environmental:
                    AddMoistureWindows(health, sensor.MacAddress, start, windowCount, windowSeconds, now);
                    break;

                case SensorCategory.PowerConsumption:
                    AddPowerWindows(health, sensor.MacAddress, start, windowCount, windowSeconds, now);
                    break;

                case SensorCategory.Actuator:
                    AddActuatorWindows(health, sensor.MacAddress, start, windowCount, windowSeconds, now);
                    break;
            }

            health.Status = OverallStatus(health, windowSeconds);
            return health;
        }

        //..............................................................................//

        //adds a window for each stretch a moisture sensor should have reported in
        private void AddMoistureWindows(SensorHealth health, string macAddress, DateTime start, int windowCount, int windowSeconds, DateTime now)
        {
            var readings = _telemetry.GetMoistureLive(macAddress);
            var windows = Bucket(readings, start, windowCount, windowSeconds);

            for (var index = 0; index < windowCount; index++)
            {
                health.Windows.Add(BuildMoistureWindow(windows[index], WindowStart(start, index, windowSeconds)));
            }

            health.SecondsSinceLastReading = SecondsSince(readings, now);
        }

        //adds a window for each stretch a power meter should have reported in
        private void AddPowerWindows(SensorHealth health, string macAddress, DateTime start, int windowCount, int windowSeconds, DateTime now)
        {
            var readings = _telemetry.GetPowerLive(macAddress);
            var windows = Bucket(readings, start, windowCount, windowSeconds);

            for (var index = 0; index < windowCount; index++)
            {
                health.Windows.Add(BuildPowerWindow(windows[index], WindowStart(start, index, windowSeconds)));
            }

            health.SecondsSinceLastReading = SecondsSince(readings, now);
        }

        //adds a window for each stretch an actuator should have reported in
        private void AddActuatorWindows(SensorHealth health, string macAddress, DateTime start, int windowCount, int windowSeconds, DateTime now)
        {
            var readings = _telemetry.GetValveLive(macAddress);
            var windows = Bucket(readings, start, windowCount, windowSeconds);

            for (var index = 0; index < windowCount; index++)
            {
                health.Windows.Add(BuildActuatorWindow(windows[index], WindowStart(start, index, windowSeconds)));
            }

            health.SecondsSinceLastReading = SecondsSince(readings, now);
        }

        //..............................................................................//

        //checks how far a moisture sensor moved during one window
        private static HealthWindow BuildMoistureWindow(List<TelemetryPacket<MoistureReading>> readings, DateTime windowStart)
        {
            var window = NewWindow(windowStart, readings.Count);

            if (readings.Count == 0)
            {
                return window;
            }

            var none = new MoistureReading(0);
            var largest = none;

            for (var index = 1; index < readings.Count; index++)
            {
                var change = readings[index].Value - readings[index - 1].Value;

                // Drying out and flooding are both faults, so a fall is made positive.
                if (change < none)
                {
                    change = none - change;
                }

                if (change > largest)
                {
                    largest = change;
                }
            }

            window.Latest = readings[readings.Count - 1].Value.ToString();
            window.LatestValue = readings[readings.Count - 1].Value.Percent;

            if (largest > new MoistureReading(MoistureSpikePercent))
            {
                window.Status = TelemetryStatus.Spike;
            }

            if (readings.Count > 1)
            {
                window.LargestChange = largest.ToString();
            }

            return window;
        }

        //checks how far a power meter moved during one window
        private static HealthWindow BuildPowerWindow(List<TelemetryPacket<PowerReading>> readings, DateTime windowStart)
        {
            var window = NewWindow(windowStart, readings.Count);

            if (readings.Count == 0)
            {
                return window;
            }

            var none = new PowerReading(0);
            var largest = none;

            for (var index = 1; index < readings.Count; index++)
            {
                var change = readings[index].Value - readings[index - 1].Value;

                // A dropped load matters as much as a pulled one.
                if (change < none)
                {
                    change = none - change;
                }

                if (change > largest)
                {
                    largest = change;
                }
            }

            window.Latest = readings[readings.Count - 1].Value.ToString();
            window.LatestValue = readings[readings.Count - 1].Value.Watts;

            if (largest > new PowerReading(PowerSpikeWatts))
            {
                window.Status = TelemetryStatus.Spike;
            }

            if (readings.Count > 1)
            {
                window.LargestChange = largest.ToString();
            }

            return window;
        }

        //checks how often an actuator changed state during one window
        private static HealthWindow BuildActuatorWindow(List<TelemetryPacket<bool>> readings, DateTime windowStart)
        {
            var window = NewWindow(windowStart, readings.Count);

            if (readings.Count == 0)
            {
                return window;
            }

            var changes = 0;

            for (var index = 1; index < readings.Count; index++)
            {
                if (readings[index].Value != readings[index - 1].Value)
                {
                    changes++;
                }
            }

            window.Latest = readings[readings.Count - 1].Value ? "Open" : "Closed";
            window.LatestValue = readings[readings.Count - 1].Value ? 1 : 0;

            // A valve is only open or closed. The fault is one that keeps flipping.
            if (changes >= ActuatorFlapChanges)
            {
                window.Status = TelemetryStatus.Spike;
            }

            if (readings.Count > 1)
            {
                window.LargestChange = changes == 1 ? "1 change" : $"{changes} changes";
            }

            return window;
        }

        //..............................................................................//

        //sorts readings into the window each one was recorded in
        private static List<TelemetryPacket<T>>[] Bucket<T>(List<TelemetryPacket<T>> readings, DateTime start, int windowCount, int windowSeconds) where T : struct
        {
            var windows = new List<TelemetryPacket<T>>[windowCount];

            for (var index = 0; index < windowCount; index++)
            {
                windows[index] = new List<TelemetryPacket<T>>();
            }

            foreach (var packet in readings)
            {
                var secondsIn = (packet.RecordedAt - start).TotalSeconds;

                // Readings before the first window, or ahead of the clock, have nowhere to sit.
                if (secondsIn < 0)
                {
                    continue;
                }

                var window = (int)(secondsIn / windowSeconds);

                if (window < windowCount)
                {
                    windows[window].Add(packet);
                }
            }

            return windows;
        }

        //..............................................................................//

        //works out where the oldest window on the ribbon begins
        private static DateTime FirstWindowStart(DateTime now, int windowCount, int windowSeconds)
        {
            // A fixed grid, or the blocks shuffle sideways on every refresh.
            var windowTicks = TimeSpan.TicksPerSecond * (long)windowSeconds;
            var currentWindow = new DateTime(now.Ticks - (now.Ticks % windowTicks), now.Kind);

            return currentWindow.AddSeconds(-(long)(windowCount - 1) * windowSeconds);
        }

        //works out where one window on the ribbon begins
        private static DateTime WindowStart(DateTime start, int index, int windowSeconds)
        {
            return start.AddSeconds((long)index * windowSeconds);
        }

        //..............................................................................//

        //builds an empty window, which counts as silent until readings are found in it
        private static HealthWindow NewWindow(DateTime windowStart, int count)
        {
            return new HealthWindow
            {
                WindowStart = windowStart,
                Count = count,
                Status = count == 0 ? TelemetryStatus.Silent : TelemetryStatus.Normal
            };
        }

        //..............................................................................//

        //works out how long ago the newest reading arrived, or null if there are none
        private static double? SecondsSince<T>(List<TelemetryPacket<T>> readings, DateTime now) where T : struct
        {
            if (readings.Count == 0)
            {
                return null;
            }

            var seconds = (now - readings[readings.Count - 1].RecordedAt).TotalSeconds;

            // A fast device clock can stamp ahead, and a negative age looks like a bug.
            return Math.Max(0, seconds);
        }

        //..............................................................................//

        //works out how the sensor is doing right now from the windows already built
        private static TelemetryStatus OverallStatus(SensorHealth health, int windowSeconds)
        {
            // Silence first. Off the air now beats spiked a few minutes ago.
            if (health.SecondsSinceLastReading == null || health.SecondsSinceLastReading > windowSeconds * SilentAfterWindows)
            {
                return TelemetryStatus.Silent;
            }

            foreach (var window in health.Windows)
            {
                if (window.Status == TelemetryStatus.Spike)
                {
                    return TelemetryStatus.Spike;
                }
            }

            return TelemetryStatus.Normal;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
