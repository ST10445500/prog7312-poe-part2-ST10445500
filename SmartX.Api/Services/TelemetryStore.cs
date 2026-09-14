using System.Collections.Concurrent;
using SmartX.Api.Collections;
using SmartX.Api.Models.Telemetry;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//TelemetryStore

//.....................................o0oSTART OF FILEo0o........................................//

// The store keeps gateway data in memory instead of using a database.

namespace SmartX.Api.Services
{
    //keeps the telemetry for every sensor, split up by the kind of reading it sends
    public class TelemetryStore
    {
        // There is a separate dictionary per category on purpose. One shared
        // dictionary would have to hold the readings as object, which boxes
        // every single one, and avoiding that is the whole reason
        // TelemetryPacket<T> is constrained to a struct.
        private readonly ConcurrentDictionary<string, SensorTelemetry<MoistureReading>> _environmental = new();

        private readonly ConcurrentDictionary<string, SensorTelemetry<PowerReading>> _power = new();

        private readonly ConcurrentDictionary<string, SensorTelemetry<bool>> _actuators = new();

        //..............................................................................//

        //records a soil moisture reading from an environmental sensor
        public void RecordMoisture(string macAddress, float percent, DateTime recordedAt)
        {
            var key = macAddress.ToUpperInvariant();
            var telemetry = _environmental.GetOrAdd(key, _ => new SensorTelemetry<MoistureReading>());
            telemetry.Add(new TelemetryPacket<MoistureReading>(key, new MoistureReading(percent), recordedAt));
        }

        //records a wattage reading from a power meter
        public void RecordPower(string macAddress, int watts, DateTime recordedAt)
        {
            var key = macAddress.ToUpperInvariant();
            var telemetry = _power.GetOrAdd(key, _ => new SensorTelemetry<PowerReading>());
            telemetry.Add(new TelemetryPacket<PowerReading>(key, new PowerReading(watts), recordedAt));
        }

        //records whether an actuator such as a valve is open or closed
        public void RecordValveState(string macAddress, bool isOpen, DateTime recordedAt)
        {
            var key = macAddress.ToUpperInvariant();
            var telemetry = _actuators.GetOrAdd(key, _ => new SensorTelemetry<bool>());
            telemetry.Add(new TelemetryPacket<bool>(key, isOpen, recordedAt));
        }

        //..............................................................................//

        //retrieves the recent moisture readings for a sensor
        public List<TelemetryPacket<MoistureReading>> GetMoistureLive(string macAddress)
        {
            return Find(_environmental, macAddress)?.GetLive() ?? new List<TelemetryPacket<MoistureReading>>();
        }

        //retrieves the recent power readings for a meter
        public List<TelemetryPacket<PowerReading>> GetPowerLive(string macAddress)
        {
            return Find(_power, macAddress)?.GetLive() ?? new List<TelemetryPacket<PowerReading>>();
        }

        //retrieves the recent valve states for an actuator
        public List<TelemetryPacket<bool>> GetValveLive(string macAddress)
        {
            return Find(_actuators, macAddress)?.GetLive() ?? new List<TelemetryPacket<bool>>();
        }

        //..............................................................................//

        //retrieves every moisture reading still held for a sensor
        public List<TelemetryPacket<MoistureReading>> GetMoistureHistory(string macAddress)
        {
            return Find(_environmental, macAddress)?.GetHistory() ?? new List<TelemetryPacket<MoistureReading>>();
        }

        //retrieves every power reading still held for a meter
        public List<TelemetryPacket<PowerReading>> GetPowerHistory(string macAddress)
        {
            return Find(_power, macAddress)?.GetHistory() ?? new List<TelemetryPacket<PowerReading>>();
        }

        //retrieves every valve state still held for an actuator
        public List<TelemetryPacket<bool>> GetValveHistory(string macAddress)
        {
            return Find(_actuators, macAddress)?.GetHistory() ?? new List<TelemetryPacket<bool>>();
        }

        //..............................................................................//

        //retrieves the newest moisture readings for a sensor
        public List<TelemetryPacket<MoistureReading>> GetMoistureHistory(string macAddress, int take)
        {
            return Find(_environmental, macAddress)?.GetHistory(take) ?? new List<TelemetryPacket<MoistureReading>>();
        }

        //retrieves the newest power readings for a meter
        public List<TelemetryPacket<PowerReading>> GetPowerHistory(string macAddress, int take)
        {
            return Find(_power, macAddress)?.GetHistory(take) ?? new List<TelemetryPacket<PowerReading>>();
        }

        //retrieves the newest valve states for an actuator
        public List<TelemetryPacket<bool>> GetValveHistory(string macAddress, int take)
        {
            return Find(_actuators, macAddress)?.GetHistory(take) ?? new List<TelemetryPacket<bool>>();
        }

        //..............................................................................//

        //retrieves how full the structures are for a sensor, or null if it has sent nothing
        public TelemetryUsage? GetUsage(string macAddress, SensorCategory category)
        {
            switch (category)
            {
                case SensorCategory.Environmental:
                    return Find(_environmental, macAddress)?.GetUsage();

                case SensorCategory.PowerConsumption:
                    return Find(_power, macAddress)?.GetUsage();

                case SensorCategory.Actuator:
                    return Find(_actuators, macAddress)?.GetUsage();

                // A category outside the three the gateway accepts means the caller
                // sent something unknown, so nothing comes back rather than actuator
                // readings that were never asked for.
                default:
                    return null;
            }
        }

        //..............................................................................//

        //retrieves how many sensors have sent telemetry so far
        public int SensorsReporting => _environmental.Count + _power.Count + _actuators.Count;

        //..............................................................................//

        //retrieves how many power meters have reported at least one reading
        public int PowerMetersReporting => _power.Count;

        //adds up what every power meter last reported, for the fleets total draw
        public PowerReading TotalPowerLoad
        {
            get
            {
                var total = new PowerReading(0);

                // This is what the + operator on PowerReading is for. Adding the
                // readings themselves keeps the wattage and its meaning together
                // instead of pulling the ints out and summing those by hand.
                foreach (var meter in _power.Values)
                {
                    if (meter.TryGetNewest(out var latest))
                    {
                        total = total + latest;
                    }
                }

                return total;
            }
        }

        //..............................................................................//

        //looks a sensor up in one of the dictionaries, or returns null
        private static SensorTelemetry<T>? Find<T>(ConcurrentDictionary<string, SensorTelemetry<T>> readings, string macAddress) where T : struct
        {
            readings.TryGetValue(macAddress.ToUpperInvariant(), out var telemetry);
            return telemetry;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
