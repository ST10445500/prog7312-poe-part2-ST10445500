//ST10445500 - PROG7312 - SmartX POE
//PumpCommand

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Api.Models.Stream
{
    //holds one manual override with the state it replaced, so it can be put back
    public class PumpCommand
    {
        //identity of the pump or valve the override went to
        public string MacAddress { get; set; } = string.Empty;

        //whether the override opened it
        public bool Open { get; set; }

        //whether it was open before the override
        public bool PreviousOpen { get; set; }

        //when the override was sent
        public DateTime SentAt { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
