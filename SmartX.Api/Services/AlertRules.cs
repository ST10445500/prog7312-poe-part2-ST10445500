using SmartX.Api.Models.Stream;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//AlertRules

//.....................................o0oSTART OF FILEo0o........................................//

// The rules decide whether a reading is urgent enough to skip the ordinary queue.

namespace SmartX.Api.Services
{
    //checks one reading against the thresholds and says what is wrong with it, if anything
    //the thresholds are hard coded for now
    public class AlertRules
    {
        //soil moisture below this percent means the plot is drying out
        public const double LowMoisturePercent = 15;

        //a meter drawing more than this many watts is overloaded, the simulator stays between 200 and 400
        public const double HighWatts = 600;

        //a signal weaker than this many dBm is close to dropping off the mesh
        public const int WeakRssi = -90;

        //..............................................................................//

        //retrieves the alert a reading raises, or null if it breaks no rule
        public StreamAlert? Evaluate(StreamPacket packet)
        {
            // Moisture and power come first because they put crops and equipment at risk.
            if (packet.Category == SensorCategory.Environmental && packet.Value < LowMoisturePercent)
            {
                return Build(packet, "LowMoisture", StreamAlert.Critical, $"Soil moisture is down to {packet.Value:0.#}%.");
            }

            if (packet.Category == SensorCategory.PowerConsumption && packet.Value > HighWatts)
            {
                return Build(packet, "HighWatts", StreamAlert.Critical, $"Meter is drawing {packet.Value:0}W.");
            }

            if (packet.Radio.Rssi != null && packet.Radio.Rssi < WeakRssi)
            {
                return Build(packet, "WeakSignal", StreamAlert.Warning, $"Signal is down to {packet.Radio.Rssi} dBm.");
            }

            return null;
        }

        //..............................................................................//

        //builds the alert for a reading that broke a rule
        private static StreamAlert Build(StreamPacket packet, string rule, int severity, string message)
        {
            return new StreamAlert
            {
                Packet = packet,
                Rule = rule,
                Severity = severity,
                Message = message,
                RaisedAt = DateTime.UtcNow
            };
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
