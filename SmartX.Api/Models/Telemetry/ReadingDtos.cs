//ST10445500 - PROG7312 - SmartX POE
//ReadingDtos

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Api.Models.Telemetry
{
    //holds one reading exactly as a device posts it to the gateway
    //which field is filled in depends on what the sensor was registered as
    public class IncomingReading
    {
        //identity of the device sending the reading
        public string MacAddress { get; set; } = string.Empty;

        //the moisture percentage or the wattage, depending on the sensor
        public double? Value { get; set; }

        //whether an actuator such as a valve is open
        public bool? State { get; set; }

        //when the device took the reading, or null to use the time it arrived
        public DateTime? RecordedAt { get; set; }
    }

    //..............................................................................//

    //holds one reading the gateway kept, in the flat shape the dashboard reads
    //every category comes back the same shape, so the client draws one thing
    public class RecordedReading
    {
        //the moisture percentage or the wattage, or null for an actuator
        public double? Value { get; set; }

        //whether the valve was open, or null for the other two categories
        public bool? State { get; set; }

        //the reading with its unit already on it, such as 42.5% or 60W
        public string Reading { get; set; } = string.Empty;

        //when the gateway recorded it
        public DateTime RecordedAt { get; set; }
    }

    //..............................................................................//

    //holds one reading the gateway turned away and the reason why
    public class RejectedReading
    {
        //where the reading sat in the batch that was posted
        public int Index { get; set; }

        //identity the device claimed, which may be blank or unregistered
        public string MacAddress { get; set; } = string.Empty;

        //what was wrong with it
        public string Reason { get; set; } = string.Empty;
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
