using SmartX.Api.Collections;
using SmartX.Api.Models.Stream;

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

        private readonly PacketQueue<StreamPacket> _queue;
        private readonly DeviceCache _cache;
        private readonly SensorStore _sensors;
        private readonly AlertQueue<StreamAlert> _alerts;
        private readonly StreamStats _stats;

        public StreamProcessor(PacketQueue<StreamPacket> queue, DeviceCache cache, SensorStore sensors, AlertQueue<StreamAlert> alerts, StreamStats stats)
        {
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

        //puts one packet into the device cache under the name the device was given
        private void Process(StreamPacket packet)
        {
            var name = _sensors.Find(packet.MacAddress)?.Location.NodeId ?? packet.MacAddress;
            _cache.Update(packet, name);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
