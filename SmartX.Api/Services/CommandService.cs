using SmartX.Api.Collections;
using SmartX.Api.Models.Stream;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//CommandService

//.....................................o0oSTART OF FILEo0o........................................//

// The service sends overrides to devices and remembers them so they can be undone.

namespace SmartX.Api.Services
{
    //sends pump and valve overrides and keeps the undo stack
    public class CommandService
    {
        private readonly TelemetryStore _telemetry;
        private readonly DeviceCache _cache;
        private readonly PacketQueue<StreamPacket> _queue;
        private readonly CommandHistory<PumpCommand> _history;

        public CommandService(TelemetryStore telemetry, DeviceCache cache, PacketQueue<StreamPacket> queue, CommandHistory<PumpCommand> history)
        {
            _telemetry = telemetry;
            _cache = cache;
            _queue = queue;
            _history = history;
        }

        //..............................................................................//

        //opens or closes an actuator and remembers what it was before
        public void Send(string macAddress, bool open)
        {
            // A device the stream has not heard from yet is taken to have been closed.
            var previous = _cache.Find(macAddress)?.State ?? false;

            Apply(macAddress, open);

            _history.Push(new PumpCommand
            {
                Label = open ? "Open" : "Close",
                SentAt = DateTime.UtcNow,
                Changes = new List<ValveChange>
                {
                    new ValveChange { MacAddress = macAddress, Open = open, PreviousOpen = previous }
                }
            });
        }

        //..............................................................................//

        //puts back every state the newest override replaced, returning false if there is nothing to undo
        public bool TryUndo(out PumpCommand? undone)
        {
            if (!_history.TryPop(out undone) || undone == null)
            {
                return false;
            }

            foreach (var change in undone.Changes)
            {
                Apply(change.MacAddress, change.PreviousOpen);
            }

            return true;
        }

        //..............................................................................//

        //records the state as a reading and puts it on the stream, the same as if the device had sent it
        private void Apply(string macAddress, bool open)
        {
            var now = DateTime.UtcNow;

            _telemetry.RecordValveState(macAddress, open, now);

            _queue.Enqueue(new StreamPacket
            {
                MacAddress = macAddress,
                Category = SensorCategory.Actuator,
                State = open,
                RecordedAt = now,
                // A command has no radio details of its own, so the last ones heard are kept.
                Radio = _cache.Find(macAddress)?.Radio ?? new RadioStats()
            });
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
