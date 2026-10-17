//ST10445500 - PROG7312 - SmartX POE
//StreamSettings

//.....................................o0oSTART OF FILEo0o........................................//

// The settings hold the numbers the stream runs on so they can change without a rebuild.

namespace SmartX.Api.Models.Stream
{
    //holds the thresholds and timings read from the Stream section of appsettings.json
    //every default is the value the stream used before it was configurable
    public class StreamSettings
    {
        // The upper limits stop a typo turning a rule off or overflowing a timer.
        private const double MaxWatts = 100000;
        private const int MaxSilentSeconds = 3600;
        private const int MaxMilliseconds = 60000;
        private const int MaxHistoryCap = 100000;

        //soil moisture below this percent means the plot is drying out
        public double LowMoisturePercent { get; set; } = 15;

        //a meter drawing more than this many watts is overloaded
        public double HighWatts { get; set; } = 600;

        //a signal weaker than this many dBm is close to dropping off the mesh
        public int WeakRssi { get; set; } = -90;

        //a device that has not reported for this many seconds is counted as gone quiet
        public int SilentSeconds { get; set; } = 15;

        //how often the processor looks for devices that have gone quiet
        public int SilentCheckMilliseconds { get; set; } = 1000;

        //how long the processor rests when both queues are empty
        public int IdleMilliseconds { get; set; } = 50;

        //the least time between two pushes to the page
        public int PushMilliseconds { get; set; } = 250;

        //how many readings are kept for each device
        public int HistoryCap { get; set; } = 500;

        //..............................................................................//

        //checks every value is usable, throwing with the name of the first one that is not
        public void Validate()
        {
            // Written as "not inside the range" so NaN is refused too.
            if (!(LowMoisturePercent >= 0 && LowMoisturePercent <= 100))
            {
                throw new InvalidOperationException("Stream:LowMoisturePercent must be between 0 and 100.");
            }

            if (!(HighWatts > 0 && HighWatts <= MaxWatts))
            {
                throw new InvalidOperationException($"Stream:HighWatts must be above 0 and no more than {MaxWatts}.");
            }

            if (WeakRssi >= 0)
            {
                throw new InvalidOperationException("Stream:WeakRssi must be below 0, signal strength is in negative dBm.");
            }

            if (SilentSeconds <= 0 || SilentSeconds > MaxSilentSeconds)
            {
                throw new InvalidOperationException($"Stream:SilentSeconds must be above 0 and no more than {MaxSilentSeconds}.");
            }

            if (SilentCheckMilliseconds <= 0 || SilentCheckMilliseconds > MaxMilliseconds)
            {
                throw new InvalidOperationException($"Stream:SilentCheckMilliseconds must be above 0 and no more than {MaxMilliseconds}.");
            }

            if (IdleMilliseconds <= 0 || IdleMilliseconds > MaxMilliseconds)
            {
                throw new InvalidOperationException($"Stream:IdleMilliseconds must be above 0 and no more than {MaxMilliseconds}.");
            }

            if (PushMilliseconds <= 0 || PushMilliseconds > MaxMilliseconds)
            {
                throw new InvalidOperationException($"Stream:PushMilliseconds must be above 0 and no more than {MaxMilliseconds}.");
            }

            if (HistoryCap <= 0 || HistoryCap > MaxHistoryCap)
            {
                throw new InvalidOperationException($"Stream:HistoryCap must be above 0 and no more than {MaxHistoryCap}.");
            }

            // A node cannot be called silent before the check that looks for it has had a turn.
            if (SilentSeconds * 1000 <= SilentCheckMilliseconds)
            {
                throw new InvalidOperationException("Stream:SilentSeconds must be longer than Stream:SilentCheckMilliseconds.");
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
