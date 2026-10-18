//ST10445500 - PROG7312 - SmartX POE
//SearchRecord

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds a search the page has settled on, so the engine can learn from it
    public class SearchRecord
    {
        //the text that was searched for
        public string Text { get; set; } = string.Empty;
    }
}

//.....................................o0oEND OF FILEo0o........................................//
