using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//ReadingGenerator

//.....................................o0oSTART OF FILEo0o........................................//

// The generator produces the values one simulated device would be reporting.

namespace SmartX.Simulator
{
    //produces readings for one sensor that drift and occasionally spike
    //one generator each, so a value carries on from where it was
    public class ReadingGenerator
    {
        //how often a reading is a deliberate spike, as one in this many
        private const int SpikeOdds = 25;

        //how quickly a stray reading is eased back toward the middle
        private const double RecoveryRate = 0.25;

        //the band a healthy moisture sensor sits in, and its step size
        private const double MinMoisture = 35;
        private const double MaxMoisture = 60;
        private const double MoistureDrift = 1.5;

        // Has to clear the gateway's fifteen percent to draw on the ribbon.
        private const double MoistureSpikeDrop = 20;

        //the band a healthy power meter sits in, and its step size
        private const double MinWatts = 200;
        private const double MaxWatts = 400;
        private const double WattDrift = 8;

        // Same idea, against the gateway's fifty watts.
        private const double WattSpikeJump = 90;

        private readonly SensorCategory _category;

        // A task per sensor means a shared Random would be drawn from at once.
        private readonly Random _random;

        //the radios a device might be built on
        private static readonly string[] Protocols = ["ESP-NOW", "Zigbee", "Thread"];

        //where this device's signal sits, so one device stays weak and another stays strong
        private readonly int _baseRssi;
        private readonly int _hopCount;
        private readonly string _protocol;

        private double _value;
        private bool _state;
        private bool _justCommanded;

        public ReadingGenerator(SensorCategory category, int seed)
        {
            _category = category;
            _random = new Random(seed);

            _baseRssi = -50 - _random.Next(36);
            _hopCount = 1 + _random.Next(3);
            _protocol = Protocols[seed % Protocols.Length];

            _value = category == SensorCategory.PowerConsumption
                ? (MinWatts + MaxWatts) / 2
                : (MinMoisture + MaxMoisture) / 2;
        }

        //..............................................................................//

        //moves the valve to the state the gateway told it to go to, so the next reading reports it
        public void SetState(bool open)
        {
            _state = open;
            _justCommanded = true;
        }

        //..............................................................................//

        //produces the next reading this sensor would report at the given time
        public DeviceReading Next(string macAddress, DateTime recordedAt)
        {
            var reading = new DeviceReading
            {
                MacAddress = macAddress,
                RecordedAt = recordedAt,
                Rssi = _baseRssi + _random.Next(-3, 4),
                HopCount = _hopCount,
                DutyCycle = Math.Round(_random.NextDouble() * 4, 1),
                PacketLoss = Math.Round(_random.NextDouble() * 3, 1),
                Protocol = _protocol
            };

            switch (_category)
            {
                case SensorCategory.Environmental:
                    reading.Value = NextMoisture();
                    break;

                case SensorCategory.PowerConsumption:
                    reading.Value = NextWatts();
                    break;

                case SensorCategory.Actuator:
                    reading.State = NextState();
                    break;
            }

            return reading;
        }

        //..............................................................................//

        //produces the next soil moisture percentage
        private double NextMoisture()
        {
            if (IsSpike())
            {
                _value -= MoistureSpikeDrop;
            }
            else
            {
                _value += Wobble(MoistureDrift);
                _value = EaseBack(_value, MinMoisture, MaxMoisture);
            }

            // A run of drops is not allowed to push this below nought.
            _value = Math.Clamp(_value, 0, 100);
            return Math.Round(_value, 1);
        }

        //..............................................................................//

        //produces the next wattage
        private double NextWatts()
        {
            if (IsSpike())
            {
                _value += WattSpikeJump;
            }
            else
            {
                _value += Wobble(WattDrift);
                _value = EaseBack(_value, MinWatts, MaxWatts);
            }

            _value = Math.Max(_value, 0);
            return Math.Round(_value);
        }

        //..............................................................................//

        //produces the next valve state, which mostly stays where it was
        private bool NextState()
        {
            // The reading straight after a command has to say what it was told, so it cannot spike.
            if (_justCommanded)
            {
                _justCommanded = false;
                return _state;
            }

            // A state change counts as a spike, at the same odds as the other two.
            if (IsSpike())
            {
                _state = !_state;
            }

            return _state;
        }

        //..............................................................................//

        //checks whether this reading should be a deliberate spike
        private bool IsSpike()
        {
            return _random.Next(SpikeOdds) == 0;
        }

        //..............................................................................//

        //produces a small random move either side of where the reading already was
        private double Wobble(double drift)
        {
            return ((_random.NextDouble() * 2) - 1) * drift;
        }

        //..............................................................................//

        //pulls a reading that has wandered outside its band back toward the middle of it
        private static double EaseBack(double value, double minimum, double maximum)
        {
            if (value >= minimum && value <= maximum)
            {
                return value;
            }

            // Without this a spiked sensor sits at its new value forever.
            var middle = (minimum + maximum) / 2;
            return value + ((middle - value) * RecoveryRate);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
