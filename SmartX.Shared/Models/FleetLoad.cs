//ST10445500 - PROG7312 - SmartX POE
//FleetLoad

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds the combined draw of every power meter, for the fleet summary line
    public class FleetLoad
    {
        //the total with its unit, already formatted by the reading itself
        public string TotalLoad { get; set; } = string.Empty;

        //how many meters went into that total
        public int MetersReporting { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
