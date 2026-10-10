//ST10445500 - PROG7312 - SmartX POE
//HistoryPoint

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds one point on a device history, as the log kept it
    public class HistoryPoint
    {
        //when the reading was taken, as the device said
        public DateTime RecordedAt { get; set; }

        //the reading with its unit already on it, such as 42.5% or 60W
        public string Reading { get; set; } = string.Empty;

        //whether the valve was open, or null for the other two categories
        public bool? State { get; set; }

        //signal strength in dBm when the reading arrived, or null if the device did not say
        public int? Rssi { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
