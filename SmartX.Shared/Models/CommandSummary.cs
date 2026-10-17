//ST10445500 - PROG7312 - SmartX POE
//CommandSummary

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds one override on the undo stack, as the page shows it
    public class CommandSummary
    {
        //what the override was, such as Shut all valves
        public string Label { get; set; } = string.Empty;

        //when the override was sent
        public DateTime SentAt { get; set; }

        //every device the override switched
        public List<CommandDeviceSummary> Devices { get; set; } = new();
    }

    //..............................................................................//

    //holds one device switched by an override and the state it replaced
    public class CommandDeviceSummary
    {
        //identity of the pump or valve the override went to
        public string MacAddress { get; set; } = string.Empty;

        //whether the override opened it
        public bool Open { get; set; }

        //whether it was open before the override
        public bool PreviousOpen { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
