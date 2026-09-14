using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Collections;
using SmartX.Api.Models.Telemetry;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//TelemetryController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    //handles readings arriving from devices and hands them back for the dashboard
    [ApiController]
    [Route("api/telemetry")]
    public class TelemetryController : ControllerBase
    {
        //the range a soil moisture sensor can report
        private const double MinMoisturePercent = 0;
        private const double MaxMoisturePercent = 100;

        //how many readings one batch may carry
        private const int MaxBatchSize = 500;

        //how many decimal places a moisture percentage is reported to
        private const int MoistureDecimals = 1;

        private readonly SensorStore _sensors;
        private readonly TelemetryStore _telemetry;
        private readonly TelemetryHealth _health;

        public TelemetryController(SensorStore sensors, TelemetryStore telemetry, TelemetryHealth health)
        {
            _sensors = sensors;
            _telemetry = telemetry;
            _health = health;
        }

        //..............................................................................//

        //records a single reading sent by a device
        [HttpPost]
        public ActionResult Record(IncomingReading reading)
        {
            var reason = TryRecord(reading);

            if (reason != null)
            {
                return BadRequest(reason);
            }

            return Accepted();
        }

        //..............................................................................//

        //records a batch of readings and reports the ones that were turned away
        [HttpPost("batch")]
        public ActionResult<BatchSummary> RecordBatch(List<IncomingReading> readings)
        {
            if (readings.Count == 0)
            {
                return BadRequest("The batch contained no readings.");
            }

            if (readings.Count > MaxBatchSize)
            {
                return BadRequest($"A batch may carry at most {MaxBatchSize} readings, and this one carried {readings.Count}.");
            }

            var result = new BatchSummary();

            for (var index = 0; index < readings.Count; index++)
            {
                var reason = TryRecord(readings[index]);

                // One bad reading should not cost the rest of the batch.
                if (reason == null)
                {
                    result.Accepted++;
                }
                else
                {
                    result.Rejected.Add(new RejectedReading
                    {
                        Index = index,
                        MacAddress = readings[index].MacAddress,
                        Reason = reason
                    });
                }
            }

            return result;
        }

        //..............................................................................//

        //retrieves the recent readings a sensor has sent
        [HttpGet("{macAddress}")]
        public ActionResult<SensorReadings> GetRecent(string macAddress)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            return new SensorReadings
            {
                MacAddress = sensor.MacAddress,
                Category = sensor.Category,
                Readings = ReadLive(sensor)
            };
        }

        //..............................................................................//

        //retrieves how full the storage structures are for a sensor
        [HttpGet("{macAddress}/usage")]
        public ActionResult<TelemetryUsage> GetUsage(string macAddress)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            var usage = _telemetry.GetUsage(sensor.MacAddress, sensor.Category);

            // Empty counters, so a sensor that has not reported reads zero and not missing.
            return usage ?? new TelemetryUsage
            {
                LiveCapacity = SensorTelemetry<bool>.LiveWindowSize
            };
        }

        //..............................................................................//

        //retrieves how a sensor has been behaving, split into equal windows of time
        [HttpGet("{macAddress}/health")]
        public ActionResult<SensorHealth> GetHealth(string macAddress, int windows = TelemetryHealth.DefaultWindowCount, int windowSeconds = TelemetryHealth.DefaultWindowSeconds)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            // A full run of silent windows, so the dashboard draws the gap.
            return _health.Build(sensor, windows, windowSeconds, DateTime.UtcNow);
        }

        //..............................................................................//

        //retrieves every registered sensor with its latest reading, for the live overview table
        [HttpGet("overview")]
        public ActionResult<List<SensorOverview>> GetOverview()
        {
            return _sensors.GetAll()
                .Select(BuildOverview)
                .ToList();
        }

        //..............................................................................//

        //retrieves the combined draw of every power meter on the fleet
        [HttpGet("load")]
        public ActionResult<FleetLoad> GetLoad()
        {
            return new FleetLoad
            {
                TotalLoad = _telemetry.TotalPowerLoad.ToString(),
                MetersReporting = _telemetry.PowerMetersReporting
            };
        }

        //..............................................................................//

        //builds one sensor's overview row from its latest usage snapshot
        private SensorOverview BuildOverview(SensorRegistration sensor)
        {
            var usage = _telemetry.GetUsage(sensor.MacAddress, sensor.Category);

            return new SensorOverview
            {
                MacAddress = sensor.MacAddress,
                Category = sensor.Category,
                Location = sensor.Location,
                LatestReading = usage?.LatestReading,
                LatestRecordedAt = usage?.LatestRecordedAt,
                // Built here to keep the overview a single round trip.
                Health = _health.Build(sensor, TelemetryHealth.DefaultWindowCount, TelemetryHealth.DefaultWindowSeconds, DateTime.UtcNow)
            };
        }

        //..............................................................................//

        //checks and stores one reading, returning why it was turned away
        private string? TryRecord(IncomingReading reading)
        {
            if (string.IsNullOrWhiteSpace(reading.MacAddress))
            {
                return "The reading did not say which device it came from.";
            }

            // Only devices the gateway knows. An unknown mac is turned away.
            var sensor = _sensors.Find(reading.MacAddress);

            if (sensor == null)
            {
                return $"No sensor is registered with MAC address {reading.MacAddress}.";
            }

            var recordedAt = reading.RecordedAt ?? DateTime.UtcNow;

            switch (sensor.Category)
            {
                case SensorCategory.Environmental:
                    return RecordMoisture(sensor, reading, recordedAt);

                case SensorCategory.PowerConsumption:
                    return RecordPower(sensor, reading, recordedAt);

                case SensorCategory.Actuator:
                    return RecordValveState(sensor, reading, recordedAt);

                default:
                    return $"{sensor.MacAddress} is registered under a category the gateway cannot store readings for.";
            }
        }

        //..............................................................................//

        //checks and stores a soil moisture reading
        private string? RecordMoisture(SensorRegistration sensor, IncomingReading reading, DateTime recordedAt)
        {
            if (reading.Value == null)
            {
                return $"{sensor.MacAddress} is an environmental sensor, so the reading needs a moisture percentage in value.";
            }

            if (reading.Value < MinMoisturePercent || reading.Value > MaxMoisturePercent)
            {
                return $"A moisture reading of {reading.Value} is outside the {MinMoisturePercent} to {MaxMoisturePercent} percent a sensor can report.";
            }

            _telemetry.RecordMoisture(sensor.MacAddress, (float)reading.Value.Value, recordedAt);
            return null;
        }

        //checks and stores a wattage reading
        private string? RecordPower(SensorRegistration sensor, IncomingReading reading, DateTime recordedAt)
        {
            if (reading.Value == null)
            {
                return $"{sensor.MacAddress} is a power meter, so the reading needs a wattage in value.";
            }

            if (reading.Value < 0)
            {
                return $"A power meter cannot draw {reading.Value} watts.";
            }

            // PowerReading holds whole watts, so a fraction is rounded.
            _telemetry.RecordPower(sensor.MacAddress, (int)Math.Round(reading.Value.Value), recordedAt);
            return null;
        }

        //checks and stores whether an actuator is open or closed
        private string? RecordValveState(SensorRegistration sensor, IncomingReading reading, DateTime recordedAt)
        {
            if (reading.State == null)
            {
                return $"{sensor.MacAddress} is an actuator, so the reading needs its open or closed state in state.";
            }

            _telemetry.RecordValveState(sensor.MacAddress, reading.State.Value, recordedAt);
            return null;
        }

        //..............................................................................//

        //retrieves the live readings for a sensor in the one flat shape the dashboard uses
        private List<RecordedReading> ReadLive(SensorRegistration sensor)
        {
            var readings = new List<RecordedReading>();

            switch (sensor.Category)
            {
                case SensorCategory.Environmental:
                    foreach (var packet in _telemetry.GetMoistureLive(sensor.MacAddress))
                    {
                        readings.Add(new RecordedReading
                        {
                            // Widening the float turns 40.1 into 40.099998474121094 on screen.
                            Value = Math.Round((double)packet.Value.Percent, MoistureDecimals),
                            Reading = packet.Value.ToString(),
                            RecordedAt = packet.RecordedAt
                        });
                    }
                    break;

                case SensorCategory.PowerConsumption:
                    foreach (var packet in _telemetry.GetPowerLive(sensor.MacAddress))
                    {
                        readings.Add(new RecordedReading
                        {
                            Value = packet.Value.Watts,
                            Reading = packet.Value.ToString(),
                            RecordedAt = packet.RecordedAt
                        });
                    }
                    break;

                case SensorCategory.Actuator:
                    foreach (var packet in _telemetry.GetValveLive(sensor.MacAddress))
                    {
                        readings.Add(new RecordedReading
                        {
                            State = packet.Value,
                            Reading = packet.Value ? "Open" : "Closed",
                            RecordedAt = packet.RecordedAt
                        });
                    }
                    break;
            }

            return readings;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
