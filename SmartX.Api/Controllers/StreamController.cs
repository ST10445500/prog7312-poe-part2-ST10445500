using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Collections;
using SmartX.Api.Models.Stream;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//StreamController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    //hands the command stream page what the processor has built up
    [ApiController]
    [Route("api/stream")]
    public class StreamController : ControllerBase
    {
        private readonly DeviceCache _cache;
        private readonly PacketQueue<StreamPacket> _queue;
        private readonly CommandHistory<PumpCommand> _history;

        private readonly AlertQueue<StreamAlert> _alerts;
        private readonly StreamStats _stats;
        private readonly HistoryLog<HistoryPoint> _log;

        public StreamController(DeviceCache cache, PacketQueue<StreamPacket> queue, CommandHistory<PumpCommand> history, AlertQueue<StreamAlert> alerts, StreamStats stats, HistoryLog<HistoryPoint> log)
        {
            _log = log;
            _cache = cache;
            _queue = queue;
            _history = history;
            _alerts = alerts;
            _stats = stats;
        }

        //..............................................................................//

        //retrieves the devices the stream has seen, narrowed to mac addresses containing the search text
        [HttpGet("devices")]
        public ActionResult<List<DeviceState>> GetDevices(string? search = null)
        {
            return Filter(search);
        }

        //..............................................................................//

        //retrieves the queue depth and the devices together, so the page polls once
        [HttpGet("snapshot")]
        public ActionResult<StreamSnapshot> GetSnapshot(string? search = null)
        {
            return new StreamSnapshot
            {
                QueueDepth = _queue.Count,
                AlertDepth = _alerts.Count,
                AlertsRaised = _stats.AlertsRaised,
                PacketsJumped = _stats.PacketsJumped,
                UndoDepth = _history.Count,
                Devices = Filter(search),
                TakenAt = DateTime.UtcNow
            };
        }

        //..............................................................................//

        //retrieves what a device reported between two moments, oldest first, or everything kept if no range is given
        [HttpGet("devices/{macAddress}/history")]
        public ActionResult<List<HistoryPoint>> GetHistory(string macAddress, DateTime? from = null, DateTime? to = null)
        {
            if (_cache.Find(macAddress) == null)
            {
                return NotFound($"The stream has not heard from {macAddress}.");
            }

            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.MaxValue;

            if (start > end)
            {
                return BadRequest("The start of the range cannot be after the end.");
            }

            return _log.GetRange(macAddress.ToUpperInvariant(), start, end);
        }

        //..............................................................................//

        //narrows the cache to the devices whose mac address contains the text
        private List<DeviceState> Filter(string? search)
        {
            var devices = _cache.GetAll();

            if (string.IsNullOrWhiteSpace(search))
            {
                return devices;
            }

            return devices
                .Where(device => device.MacAddress.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
