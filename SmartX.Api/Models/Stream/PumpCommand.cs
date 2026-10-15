//ST10445500 - PROG7312 - SmartX POE
//PumpCommand

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Api.Models.Stream
{
    //holds one manual override, which may switch several devices, so a single undo can put them all back
    public class PumpCommand
    {
        //what the override was, such as Shut all valves
        public string Label { get; set; } = string.Empty;

        //when the override was sent
        public DateTime SentAt { get; set; }

        //every device the override switched, with the state each one had before
        public List<ValveChange> Changes { get; set; } = new();
    }

    //..............................................................................//

    //holds one device switched by an override and the state it replaced
    public class ValveChange
    {
        //identity of the pump or valve the override went to
        public string MacAddress { get; set; } = string.Empty;

        //whether the override opened it
        public bool Open { get; set; }

        //whether it was open before the override
        public bool PreviousOpen { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
