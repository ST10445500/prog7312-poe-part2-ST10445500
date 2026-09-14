using SmartX.Api.Models.Telemetry;

//ST10445500 - PROG7312 - SmartX POE
//SensorTelemetry

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so the gateway can control how memory is used.

namespace SmartX.Api.Collections
{
    //holds everything the gateway keeps for one sensor
    //recent readings go in the ring buffer and the rest go in the batch grid
    public class SensorTelemetry<T> where T : struct
    {
        //how many recent readings the dashboard can show at once
        //at a reading every thirty seconds this is one hour of live view per sensor
        public const int LiveWindowSize = 120;

        // The live window, the grid and the list together hold about six thousand
        // readings per sensor, which comes to roughly 150 KB. Every stage is capped
        // so a gateway that is left running does not fill up.
        private readonly RingBuffer<TelemetryPacket<T>> _live = new RingBuffer<TelemetryPacket<T>>(LiveWindowSize);

        private readonly TelemetryBatches<T> _history = new TelemetryBatches<T>();

        // The ring buffer and the batch grid are plain single threaded classes.
        // Two readings arriving for the same sensor at once are kept apart here
        // instead of complicating those two.
        private readonly object _lock = new object();

        //..............................................................................//

        //adds a reading to the live window and to the history
        public void Add(TelemetryPacket<T> packet)
        {
            lock (_lock)
            {
                // The reading is kept in both places on purpose. The dashboard asks for
                // the live window over and over, so holding a small copy there means
                // those reads never have to go near the much larger history.
                _live.Add(packet);
                _history.Add(packet);
            }
        }

        //..............................................................................//

        //retrieves the most recent readings, oldest first
        public List<TelemetryPacket<T>> GetLive()
        {
            lock (_lock)
            {
                return _live.ToList();
            }
        }

        //..............................................................................//

        //retrieves every reading still held for this sensor, oldest first
        public List<TelemetryPacket<T>> GetHistory()
        {
            lock (_lock)
            {
                return _history.ToList();
            }
        }

        //..............................................................................//

        //retrieves the newest readings held for this sensor, oldest first
        public List<TelemetryPacket<T>> GetHistory(int take)
        {
            // Asking for a set number keeps the lock held for a short copy instead of
            // a few thousand readings, so new readings are not held up behind a read.
            lock (_lock)
            {
                return _history.GetNewest(take);
            }
        }

        //..............................................................................//

        //retrieves the newest reading, or false if the sensor has sent nothing yet
        public bool TryGetNewest(out T value)
        {
            lock (_lock)
            {
                if (_live.Count == 0)
                {
                    value = default;
                    return false;
                }

                value = _live.Newest().Value;
                return true;
            }
        }

        //..............................................................................//

        //retrieves how full each of the structures is
        public TelemetryUsage GetUsage()
        {
            lock (_lock)
            {
                var usage = new TelemetryUsage
                {
                    LiveReadings = _live.Count,
                    LiveCapacity = _live.Capacity,
                    FinishedBatches = _history.FinishedBatchCount,
                    ReadingsInCurrentBatch = _history.ReadingsInCurrentBatch,
                    TransferredToList = _history.TransferredCount
                };

                // A sensor that has sent nothing has no newest reading to report, so
                // the two fields are left null rather than showing a made up value.
                if (_live.Count > 0)
                {
                    var latest = _live.Newest();

                    usage.LatestReading = latest.Value.ToString();
                    usage.LatestRecordedAt = latest.RecordedAt;
                }

                return usage;
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
