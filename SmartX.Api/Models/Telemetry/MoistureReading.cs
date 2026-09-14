//ST10445500 - PROG7312 - SmartX POE
//MoistureReading

//.....................................o0oSTART OF FILEo0o........................................//

// This model represents data that the gateway stores and works with in memory.

namespace SmartX.Api.Models.Telemetry
{
    //holds a soil moisture reading as a percentage
    //there is no + operator because adding two moisture percentages together means nothing
    public readonly struct MoistureReading
    {
        //how close two readings have to be before they count as the same
        public const float Tolerance = 0.1f;

        //the percentage the sensor reported
        public float Percent { get; }

        //..............................................................................//

        public MoistureReading(float percent)
        {
            Percent = percent;
        }

        //..............................................................................//

        //subtracts one reading from another to get how far the moisture moved
        public static MoistureReading operator -(MoistureReading left, MoistureReading right)
        {
            return new MoistureReading(left.Percent - right.Percent);
        }

        //..............................................................................//

        //checks whether the left reading is wetter
        public static bool operator >(MoistureReading left, MoistureReading right)
        {
            return left.Percent > right.Percent;
        }

        //checks whether the left reading is drier
        public static bool operator <(MoistureReading left, MoistureReading right)
        {
            return left.Percent < right.Percent;
        }

        //..............................................................................//

        //checks whether two readings are close enough to count as the same
        public static bool operator ==(MoistureReading left, MoistureReading right)
        {
            // A sensor drifts between readings, so an exact float match would never hit.
            return Math.Abs(left.Percent - right.Percent) < Tolerance;
        }

        //checks whether two readings are far enough apart to count as different
        public static bool operator !=(MoistureReading left, MoistureReading right)
        {
            return !(left == right);
        }

        //..............................................................................//

        //checks this reading against another object using the same tolerance
        public override bool Equals(object? obj)
        {
            return obj is MoistureReading other && this == other;
        }

        //retrieves a hash code that agrees with the tolerance the equals operator uses
        public override int GetHashCode()
        {
            // Readings inside the tolerance have to hash alike. This is never used as a key.
            return 0;
        }

        //..............................................................................//

        //retrieves the reading with its unit
        public override string ToString()
        {
            return $"{Percent:0.0}%";
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
