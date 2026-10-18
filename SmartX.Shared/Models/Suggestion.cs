//ST10445500 - PROG7312 - SmartX POE
//Suggestion

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds one thing the engine thinks the operator should look at or do next
    public class Suggestion
    {
        //Problem for a device that needs attention now, Next for what usually follows the last thing done
        public string Kind { get; set; } = string.Empty;

        //the event it comes from, such as command:open:AA:BB:CC:DD:EE:02
        public string Event { get; set; } = string.Empty;

        //what to do, such as Open valve-a1
        public string Text { get; set; } = string.Empty;

        //how sure the engine is, from 0 to 1
        public double Confidence { get; set; }

        //a plain sentence saying why
        public string Reason { get; set; } = string.Empty;

        //the pump or valve it is about, if it is about one
        public string? MacAddress { get; set; }

        //true to open it, false to close it, null if it is not a switch
        public bool? Open { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
