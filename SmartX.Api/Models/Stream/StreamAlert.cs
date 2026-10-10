//ST10445500 - PROG7312 - SmartX POE
//StreamAlert

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Api.Models.Stream
{
    //holds a reading that broke a rule, together with why, while it waits to be processed
    public class StreamAlert
    {
        //how urgent each kind of alert is, the bigger number leaves the alert queue first
        public const int Warning = 1;
        public const int Critical = 2;

        //the reading that tripped the rule
        public StreamPacket Packet { get; set; } = new();

        //which rule it tripped, such as LowMoisture
        public string Rule { get; set; } = string.Empty;

        //Warning or Critical
        public int Severity { get; set; }

        //a plain sentence saying what was wrong with the reading
        public string Message { get; set; } = string.Empty;

        //when the gateway noticed
        public DateTime RaisedAt { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
