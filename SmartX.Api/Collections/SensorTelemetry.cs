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

        // About six thousand readings a sensor, roughly 150 KB. Every stage is capped.
        private readonly RingBuffer<TelemetryPacket<T>> _live = new RingBuffer<TelemetryPacket<T>>(LiveWindowSize);

        private readonly TelemetryBatches<T> _history = new TelemetryBatches<T>();

        // The buffer and the grid are single threaded. This lock keeps them that way.
        private readonly object _lock = new object();

        //..............................................................................//

        //adds a reading to the live window and to the history
        public void Add(TelemetryPacket<T> packet)
        {
            lock (_lock)
            {
                // Kept in both. The dashboard hits the live window constantly.
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
            // A set number keeps the lock held for a short copy.
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
