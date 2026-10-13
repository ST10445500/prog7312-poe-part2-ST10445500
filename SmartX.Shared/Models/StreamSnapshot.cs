//ST10445500 - PROG7312 - SmartX POE
//StreamSnapshot

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds everything the command stream page draws, in one round trip
    public class StreamSnapshot
    {
        //how many packets are waiting to be processed
        public int QueueDepth { get; set; }

        //how many alerts are waiting to be processed
        public int AlertDepth { get; set; }

        //how many alerts have been processed since the api started
        public long AlertsRaised { get; set; }

        //how many ordinary packets were still waiting when those alerts were processed
        public long PacketsJumped { get; set; }

        //how many faults are active right now
        public int ActiveErrors { get; set; }

        //how many repeats of an active fault were turned away instead of alerted
        public long SuppressedDuplicates { get; set; }

        //the devices that have gone quiet
        public List<string> DisconnectedNodes { get; set; } = new();

        //how many overrides can still be undone
        public int UndoDepth { get; set; }

        //every device the stream has seen, narrowed by the search text if one was sent
        public List<DeviceState> Devices { get; set; } = new();

        //when the gateway built this snapshot
        public DateTime TakenAt { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
