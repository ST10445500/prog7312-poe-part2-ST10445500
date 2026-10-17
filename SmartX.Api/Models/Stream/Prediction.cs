//ST10445500 - PROG7312 - SmartX POE
//Prediction

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Api.Models.Stream
{
    //holds one event that has followed another, with how often and how sure the model is of it
    public class Prediction
    {
        //the event that came next, such as command:open:AA:BB:CC:DD:EE:01
        public string Event { get; set; } = string.Empty;

        //how many times it followed
        public int Count { get; set; }

        //the share of all the times the earlier event was followed by something, from 0 to 1
        public double Confidence { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
