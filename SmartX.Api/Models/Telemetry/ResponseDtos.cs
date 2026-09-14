using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//ResponseDtos

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Api.Models.Telemetry
{
    //holds the recent readings for one sensor along with enough about it to label them
    public class SensorReadings
    {
        //identity of the device the readings came from
        public string MacAddress { get; set; } = string.Empty;

        //what kind of readings these are
        public SensorCategory Category { get; set; }

        //the readings themselves, oldest first
        public List<RecordedReading> Readings { get; set; } = new List<RecordedReading>();
    }

    //..............................................................................//

    //holds what happened to a batch of readings
    //a real gateway keeps the good readings out of a bad batch
    public class BatchSummary
    {
        //how many readings were stored
        public int Accepted { get; set; }

        //the readings that were turned away, with a reason each
        public List<RejectedReading> Rejected { get; set; } = new List<RejectedReading>();

        //how many readings were posted in total
        public int Received => Accepted + Rejected.Count;
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
