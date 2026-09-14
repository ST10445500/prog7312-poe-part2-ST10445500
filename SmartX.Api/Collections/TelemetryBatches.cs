using SmartX.Api.Models.Telemetry;

//ST10445500 - PROG7312 - SmartX POE
//TelemetryBatches

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so the gateway can control how memory is used.

namespace SmartX.Api.Collections
{
    //holds the historical readings for one sensor in a grid of batches
    //readings fill a row, and a full grid moves into a list
    public class TelemetryBatches<T> where T : struct
    {
        //how many readings go into one batch
        public const int BatchSize = 50;

        //how many batches the grid holds before they are transferred to the list
        //fifty readings across twenty rows means a thousand are staged before each transfer
        public const int MaxBatches = 20;

        //how many readings the list keeps before the oldest ones are dropped
        //at a reading every thirty seconds this works out to about forty one hours per sensor
        public const int MaxTransferred = 5000;

        // One block of memory, allocated once. Gateways are short on it.
        private readonly TelemetryPacket<T>[,] _batches = new TelemetryPacket<T>[MaxBatches, BatchSize];

        //which row is being filled at the moment
        private int _row;

        //the next free slot in that row
        private int _column;

        //where the readings end up once the grid has filled
        private readonly List<TelemetryPacket<T>> _transferred = new List<TelemetryPacket<T>>();

        //..............................................................................//

        //retrieves how many rows are completely full
        public int FinishedBatchCount => _row;

        //retrieves how many readings are in the row being filled
        public int ReadingsInCurrentBatch => _column;

        //retrieves how many readings have been transferred into the list
        public int TransferredCount => _transferred.Count;

        //..............................................................................//

        //adds a reading, starting a new row when the current one fills
        public void Add(TelemetryPacket<T> packet)
        {
            _batches[_row, _column] = packet;
            _column++;

            if (_column < BatchSize)
            {
                return;
            }

            _column = 0;
            _row++;

            if (_row == MaxBatches)
            {
                TransferBatchesToList();
            }
        }

        //..............................................................................//

        //retrieves every reading held for this sensor, oldest first
        public List<TelemetryPacket<T>> ToList()
        {
            var all = new List<TelemetryPacket<T>>(_transferred);

            for (var row = 0; row < _row; row++)
            {
                for (var column = 0; column < BatchSize; column++)
                {
                    all.Add(_batches[row, column]);
                }
            }

            for (var column = 0; column < _column; column++)
            {
                all.Add(_batches[_row, column]);
            }

            return all;
        }

        //..............................................................................//

        //retrieves the newest readings held for this sensor, oldest first
        public List<TelemetryPacket<T>> GetNewest(int count)
        {
            var available = _transferred.Count + (_row * BatchSize) + _column;
            var wanted = Math.Min(count, available);

            if (wanted < 1)
            {
                return new List<TelemetryPacket<T>>();
            }

            var newest = new List<TelemetryPacket<T>>(wanted);

            // Backwards from the newest, so only the wanted readings get copied.
            for (var column = _column - 1; column >= 0 && newest.Count < wanted; column--)
            {
                newest.Add(_batches[_row, column]);
            }

            for (var row = _row - 1; row >= 0 && newest.Count < wanted; row--)
            {
                for (var column = BatchSize - 1; column >= 0 && newest.Count < wanted; column--)
                {
                    newest.Add(_batches[row, column]);
                }
            }

            for (var i = _transferred.Count - 1; i >= 0 && newest.Count < wanted; i--)
            {
                newest.Add(_transferred[i]);
            }

            // Collected newest first, so flip it back.
            newest.Reverse();
            return newest;
        }

        //..............................................................................//

        //transfers the finished rows out of the grid and into the list
        private void TransferBatchesToList()
        {
            // The grid is a staging area. Full means move to the list and start again.
            for (var row = 0; row < MaxBatches; row++)
            {
                for (var column = 0; column < BatchSize; column++)
                {
                    _transferred.Add(_batches[row, column]);
                }
            }

            // The list cannot grow forever either, so the oldest fall off the front.
            if (_transferred.Count > MaxTransferred)
            {
                _transferred.RemoveRange(0, _transferred.Count - MaxTransferred);
            }

            _row = 0;
            _column = 0;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
