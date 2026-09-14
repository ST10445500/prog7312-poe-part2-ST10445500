//ST10445500 - PROG7312 - SmartX POE
//PowerReading

//.....................................o0oSTART OF FILEo0o........................................//

// This model represents data that the gateway stores and works with in memory.

namespace SmartX.Api.Models.Telemetry
{
    //holds a power reading in watts
    //meters add together for the fleets total draw, which is what + is for
    public readonly struct PowerReading
    {
        //how many watts the meter reported
        public int Watts { get; }

        //..............................................................................//

        public PowerReading(int watts)
        {
            Watts = watts;
        }

        //..............................................................................//

        //adds two meter readings into one combined load
        public static PowerReading operator +(PowerReading left, PowerReading right)
        {
            return new PowerReading(left.Watts + right.Watts);
        }

        //subtracts one reading from another to get the change in load
        public static PowerReading operator -(PowerReading left, PowerReading right)
        {
            return new PowerReading(left.Watts - right.Watts);
        }

        //..............................................................................//

        //checks whether the left reading is drawing more power
        public static bool operator >(PowerReading left, PowerReading right)
        {
            return left.Watts > right.Watts;
        }

        //checks whether the left reading is drawing less power
        public static bool operator <(PowerReading left, PowerReading right)
        {
            return left.Watts < right.Watts;
        }

        //..............................................................................//

        //checks whether two meters reported the same wattage
        public static bool operator ==(PowerReading left, PowerReading right)
        {
            // Wattage is a whole number, so unlike a moisture percentage this can be
            // compared exactly and needs no tolerance.
            return left.Watts == right.Watts;
        }

        //checks whether two meters reported different wattages
        public static bool operator !=(PowerReading left, PowerReading right)
        {
            return !(left == right);
        }

        //..............................................................................//

        //checks this reading against another object
        public override bool Equals(object? obj)
        {
            return obj is PowerReading other && this == other;
        }

        //retrieves a hash code based on the wattage
        public override int GetHashCode()
        {
            return Watts.GetHashCode();
        }

        //..............................................................................//

        //retrieves the reading with its unit
        public override string ToString()
        {
            return $"{Watts}W";
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
