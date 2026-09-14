//ST10445500 - PROG7312 - SmartX POE
//HealthDtos

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds how a sensor has been behaving, split into a run of equal windows of time
    public class SensorHealth
    {
        //identity of the device the windows describe
        public string MacAddress { get; set; } = string.Empty;

        //what kind of sensor it is, which decides what counts as a sharp change
        public SensorCategory Category { get; set; }

        //how many seconds of readings each window covers
        public int WindowSeconds { get; set; }

        //when the gateway worked these windows out
        public DateTime GeneratedAt { get; set; }

        //how the sensor is doing right now rather than across the whole run
        public TelemetryStatus Status { get; set; }

        //how long ago the newest reading arrived, or null if the sensor has never reported
        public double? SecondsSinceLastReading { get; set; }

        //the windows themselves, oldest first
        public List<HealthWindow> Windows { get; set; } = new List<HealthWindow>();
    }

    //..............................................................................//

    //holds what one sensor did during one window of time
    public class HealthWindow
    {
        //when this window starts
        public DateTime WindowStart { get; set; }

        //how many readings arrived during it
        public int Count { get; set; }

        //whether the window was silent, ordinary, or held a sharp change
        public TelemetryStatus Status { get; set; }

        //the biggest change inside the window, or null if there was nothing to compare
        public string? LargestChange { get; set; }

        //the newest reading in the window, or null if none arrived
        public string? Latest { get; set; }

        //the newest reading in the window as a plain number, for drawing a sparkline
        public double? LatestValue { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
