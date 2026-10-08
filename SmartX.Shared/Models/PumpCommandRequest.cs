//ST10445500 - PROG7312 - SmartX POE
//PumpCommandRequest

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds a manual override as the page sends it
    public class PumpCommandRequest
    {
        //identity of the pump or valve to send it to
        public string MacAddress { get; set; } = string.Empty;

        //true to open it, false to close it
        public bool Open { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
