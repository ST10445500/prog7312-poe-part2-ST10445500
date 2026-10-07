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

        public StreamProcessor(PacketQueue<StreamPacket> queue, DeviceCache cache, SensorStore sensors)
        {
            _queue = queue;
            _cache = cache;
            _sensors = sensors;
        }

        //..............................................................................//

        //keeps draining the queue until the api shuts down
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var packet) && packet != null)
                {
                    var name = _sensors.Find(packet.MacAddress)?.Location.NodeId ?? packet.MacAddress;
                    _cache.Update(packet, name);
                    continue;
                }

                await Task.Delay(IdleMilliseconds, stoppingToken);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
