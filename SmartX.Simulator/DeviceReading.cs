//ST10445500 - PROG7312 - SmartX POE
//DeviceReading

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Simulator
{
    //holds one reading in the shape the gateway expects a device to post
    //declared here too, because the simulator is a separate device
    public class DeviceReading
    {
        //identity of the device sending the reading
        public string MacAddress { get; set; } = string.Empty;

        //the moisture percentage or the wattage, depending on the sensor
        public double? Value { get; set; }

        //whether an actuator such as a valve is open
        public bool? State { get; set; }

        //when the device took the reading, which the gateway keeps instead of the arrival time
        public DateTime? RecordedAt { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
