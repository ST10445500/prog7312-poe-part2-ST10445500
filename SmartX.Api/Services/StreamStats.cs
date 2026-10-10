//ST10445500 - PROG7312 - SmartX POE
//StreamStats

//.....................................o0oSTART OF FILEo0o........................................//

// The counters keep running totals in memory so the page can show what the priority path has done.

namespace SmartX.Api.Services
{
    //counts alerts and the packets they overtook, safe to bump from the processor while requests read
    public class StreamStats
    {
        private long _alertsRaised;
        private long _packetsJumped;

        //..............................................................................//

        //retrieves how many alerts have been processed
        public long AlertsRaised => Interlocked.Read(ref _alertsRaised);

        //retrieves how many ordinary packets were still waiting when an alert was processed
        public long PacketsJumped => Interlocked.Read(ref _packetsJumped);

        //..............................................................................//

        //records an alert going through and how many packets it went ahead of
        public void RecordAlert(int packetsWaiting)
        {
            Interlocked.Increment(ref _alertsRaised);
            Interlocked.Add(ref _packetsJumped, packetsWaiting);
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
