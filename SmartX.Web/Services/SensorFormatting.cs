using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//SensorFormatting

//.....................................o0oSTART OF FILEo0o........................................//

// The helper keeps shared client wording and settings out of the pages that use them.

namespace SmartX.Web.Services
{
    //the wording every page uses for a device
    public static class SensorFormatting
    {
        //where a device sits, or that it has no place yet
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

        //names a registered device for a dropdown
        public static string DescribeSensor(SensorRegistration sensor)
        {
            return DescribeSensor(sensor.Location.NodeId, sensor.MacAddress, sensor.Category);
        }

        //..............................................................................//

        //the same wording, for a device that has reported
        public static string DescribeSensor(SensorOverview sensor)
        {
            return DescribeSensor(sensor.Location.NodeId, sensor.MacAddress, sensor.Category);
        }

        //..............................................................................//

        private static string DescribeSensor(string nodeId, string macAddress, SensorCategory category)
        {
            // An unnamed device would otherwise show empty brackets.
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return $"{macAddress} - {category}";
            }

            return $"{nodeId} ({macAddress}) - {category}";
        }

        //..............................................................................//

        //the newest reading, or a dash
        public static string DescribeLatestReading(SensorOverview sensor)
        {
            // ToString gives True or False, and a valve is open or closed.
            if (sensor.Category == SensorCategory.Actuator && sensor.LatestReading != null)
            {
                return sensor.LatestReading == "True" ? "Open" : "Closed";
            }

            return sensor.LatestReading ?? "-";
        }

        //..............................................................................//

        //how long ago a device last reported
        public static string DescribeLastSeen(SensorHealth health)
        {
            if (health.SecondsSinceLastReading == null)
            {
                return "no readings yet";
            }

            return $"{DescribeDuration((int)health.SecondsSinceLastReading.Value)} ago";
        }

        //..............................................................................//

        //a span of time in the largest unit that still reads sensibly
        public static string DescribeDuration(int seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds}s";
            }

            if (seconds < 3600)
            {
                return $"{seconds / 60}m";
            }

            return $"{seconds / 3600}h";
        }

        //..............................................................................//

        //what is wrong with a typed reading, or null if it is fine
        public static string? DescribeReadingProblem(SensorCategory? category, double? value)
        {
            // An actuator carries a state, and the dropdown cannot be empty.
            if (category == SensorCategory.Actuator)
            {
                return null;
            }

            if (value == null)
            {
                return "Enter a reading first.";
            }

            // Same bounds TelemetryController enforces.
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
