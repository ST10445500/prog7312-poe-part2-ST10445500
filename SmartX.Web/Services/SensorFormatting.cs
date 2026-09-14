using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//SensorFormatting

//.....................................o0oSTART OF FILEo0o........................................//

// The service keeps business rules and validation away from the controller.

namespace SmartX.Web.Services
{
    //turns sensor data into the wording the pages show, so every page says it the same way
    public static class SensorFormatting
    {
        //describes where a sensor sits, or says so when it is not in the tree yet
        public static string DescribeLocation(DeploymentLocation location)
        {
            if (string.IsNullOrWhiteSpace(location.Zone))
            {
                return "Not placed";
            }

            if (string.Equals(location.Zone, location.Room, StringComparison.OrdinalIgnoreCase))
            {
                return location.Zone;
            }

            return $"{location.Zone} / {location.Room}";
        }

        //..............................................................................//

        //describes the newest reading a sensor sent, or a dash when it has not reported
        public static string DescribeLatestReading(SensorOverview sensor)
        {
            // The reading struct's own ToString gives True or False for an actuator,
            // and the rest of the app talks about valves being open or closed.
            if (sensor.Category == SensorCategory.Actuator && sensor.LatestReading != null)
            {
                return sensor.LatestReading == "True" ? "Open" : "Closed";
            }

            return sensor.LatestReading ?? "-";
        }

        //..............................................................................//

        //describes what is wrong with a hand typed reading, or null when it is ready to post
        public static string? DescribeReadingProblem(SensorCategory? category, double? value)
        {
            // An actuator carries a state rather than a number, and the dropdown
            // cannot be left empty, so there is nothing to check.
            if (category == SensorCategory.Actuator)
            {
                return null;
            }

            if (value == null)
            {
                return "Enter a reading first.";
            }

            // These are the same bounds TelemetryController enforces, checked here
            // so the gateway never has to turn the reading away.
            if (category == SensorCategory.Environmental && value is < 0 or > 100)
            {
                return "A moisture reading has to be between 0 and 100 percent.";
            }

            if (category == SensorCategory.PowerConsumption && value < 0)
            {
                return "A power meter cannot draw less than nothing.";
            }

            return null;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
