using SmartX.Api.Collections;
using SmartX.Api.Models.Stream;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//StreamProcessor

//.....................................o0oSTART OF FILEo0o........................................//

// The processor runs in the background so a request never waits on the work done after it.

namespace SmartX.Api.Services
{
    //takes packets off the queue and updates the device cache with them
    public class StreamProcessor : BackgroundService
    {
        // How long to rest when the queue is empty. Hard coded for now.
        private const int IdleMilliseconds = 50;

        // How often to look for devices that have gone quiet. Hard coded for now.
        private const int SilentCheckMilliseconds = 1000;

        private readonly PacketQueue<StreamPacket> _queue;
        private readonly DeviceCache _cache;
        private readonly SensorStore _sensors;
        private readonly AlertQueue<StreamAlert> _alerts;
        private readonly StreamStats _stats;
        private readonly HistoryLog<HistoryPoint> _history;
        private readonly ActiveFaults _faults;
        private readonly AlertRules _rules;

        // When the silent check last ran, so it runs about once a second and not on every packet.
        private long _lastSilentCheck;

        public StreamProcessor(PacketQueue<StreamPacket> queue, DeviceCache cache, SensorStore sensors, AlertQueue<StreamAlert> alerts, StreamStats stats, HistoryLog<HistoryPoint> history, ActiveFaults faults, AlertRules rules)
        {
            _history = history;
            _faults = faults;
            _rules = rules;
            _queue = queue;
            _cache = cache;
            _sensors = sensors;
            _alerts = alerts;
            _stats = stats;
        }

        //..............................................................................//

        //keeps draining the queue until the api shuts down
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CheckSilent();

                // Alerts go first, whatever is waiting on the ordinary queue.
                if (_alerts.TryDequeue(out var alert) && alert != null)
                {
                    _stats.RecordAlert(_queue.Count);
                    Process(alert.Packet);
                    continue;
                }

                if (_queue.TryDequeue(out var packet) && packet != null)
                {
                    Process(packet);
                    continue;
                }

                await Task.Delay(IdleMilliseconds, stoppingToken);
            }
        }

        //..............................................................................//

        //marks the devices that have gone quiet, checking at most once a second
        private void CheckSilent()
        {
            var now = Environment.TickCount64;

            if (now - _lastSilentCheck < SilentCheckMilliseconds)
            {
                return;
            }

            _lastSilentCheck = now;

            foreach (var mac in _rules.FindSilent(_cache.GetAll(), DateTime.UtcNow))
            {
                // Already marked devices come back false, so a quiet device is only noted once.
                _faults.MarkDisconnected(mac);
            }
        }

        //..............................................................................//

        //puts one packet into the device cache under the name the device was given
        private void Process(StreamPacket packet)
        {
            // Any packet means the device is talking again, which re-arms its disconnect.
            _faults.MarkConnected(packet.MacAddress);

            var name = _sensors.Find(packet.MacAddress)?.Location.NodeId ?? packet.MacAddress;
            _cache.Update(packet, name);

            // The cache has just built the reading text with its unit, so the log reuses it.
            var state = _cache.Find(packet.MacAddress);

            _history.Add(packet.MacAddress.ToUpperInvariant(), packet.RecordedAt, new HistoryPoint
            {
                RecordedAt = packet.RecordedAt,
                Reading = state?.Reading ?? string.Empty,
                State = packet.State,
                Rssi = packet.Radio.Rssi
            });
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
