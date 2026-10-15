//ST10445500 - PROG7312 - SmartX POE
//DeviceState

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds what the stream last knew about one device
    public class DeviceState
    {
        //identity of the device
        public string MacAddress { get; set; } = string.Empty;

        //the name the device was given when it was added
        public string Name { get; set; } = string.Empty;

        public SensorCategory Category { get; set; }

        //the latest reading with its unit already on it, such as 42.5% or 60W
        public string Reading { get; set; } = string.Empty;

        //whether the valve was open, or null for the other two categories
        public bool? State { get; set; }

        //when the latest reading was taken
        public DateTime LastSeen { get; set; }

        public RadioStats Radio { get; set; } = new();

        //the fault codes active on the device right now, empty if it has none
        public List<string> Faults { get; set; } = new();

        //whether the device has gone quiet
        public bool Disconnected { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
