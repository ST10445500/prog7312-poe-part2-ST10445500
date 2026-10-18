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
        private readonly ActiveFaults _faults;

        public StreamController(DeviceCache cache, PacketQueue<StreamPacket> queue, CommandHistory<PumpCommand> history, AlertQueue<StreamAlert> alerts, StreamStats stats, HistoryLog<HistoryPoint> log, ActiveFaults faults)
        {
            _faults = faults;
            _log = log;
            _cache = cache;
            _queue = queue;
            _history = history;
            _alerts = alerts;
            _stats = stats;
        }

        //..............................................................................//

        //retrieves the devices the stream has seen, narrowed by search text, category and whether they have a fault
        [HttpGet("devices")]
        public ActionResult<List<DeviceState>> GetDevices(string? search = null, SensorCategory? category = null, bool alertsOnly = false)
        {
            return Filter(search, category, alertsOnly);
        }

        //..............................................................................//

        //retrieves the queue depth and the devices together, so the page polls once
        [HttpGet("snapshot")]
        public ActionResult<StreamSnapshot> GetSnapshot(string? search = null, SensorCategory? category = null, bool alertsOnly = false)
        {
            return new StreamSnapshot
            {
                QueueDepth = _queue.Count,
                AlertDepth = _alerts.Count,
                AlertsRaised = _stats.AlertsRaised,
                PacketsJumped = _stats.PacketsJumped,
                ActiveErrors = _faults.ActiveErrorCount,
                SuppressedDuplicates = _faults.SuppressedCount,
                DisconnectedNodes = _faults.GetDisconnected(),
                UndoDepth = _history.Count,
                Devices = Filter(search, category, alertsOnly),
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

        //narrows the cache to the devices that match the search text, the category and the alert switch together
        private List<DeviceState> Filter(string? search, SensorCategory? category, bool alertsOnly)
        {
            var devices = Describe(_cache.GetAll());

            if (!string.IsNullOrWhiteSpace(search))
            {
                devices = devices
                    .Where(device => device.MacAddress.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (category != null)
            {
                devices = devices
                    .Where(device => device.Category == category)
                    .ToList();
            }

            if (alertsOnly)
            {
                devices = devices
                    .Where(device => device.Faults.Count > 0 || device.Disconnected)
                    .ToList();
            }

            return devices;
        }

        //..............................................................................//

        //copies each device with its active faults and whether it has gone quiet
        private List<DeviceState> Describe(List<DeviceState> devices)
        {
            // Grouped once by mac, so each device looks its codes up and does not walk every active fault.
            var faults = _faults.GetErrors()
                .GroupBy(fault => fault.Mac)
                .ToDictionary(group => group.Key, group => group.Select(fault => fault.Code).ToList());
            var quiet = _faults.GetDisconnected().ToHashSet();

            // The cache hands out the objects it stores, so these are copies and the cache is left alone.
            return devices
                .Select(device => new DeviceState
                {
                    MacAddress = device.MacAddress,
                    Name = device.Name,
                    Category = device.Category,
                    Reading = device.Reading,
                    State = device.State,
                    LastSeen = device.LastSeen,
                    Radio = device.Radio,
                    Faults = faults.TryGetValue(device.MacAddress, out var codes) ? codes : new List<string>(),
                    Disconnected = quiet.Contains(device.MacAddress)
                })
                .ToList();
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
