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

        //how long each profile's pattern takes to repeat, counted in readings
        private const int ThirstyCycle = 90;
        private const int SurgeCycle = 100;
        private const int WeakCycle = 120;
        private const int FlakyCycle = 40;
        private const int FaultCycle = 80;

        //how long a quiet device stays quiet, and how often, in seconds
        private const int QuietCycleSeconds = 120;
        private const int QuietStartSeconds = 40;
        private const int QuietLengthSeconds = 30;

        // Well over the gateway's six hundred watts.
        private const double WattSurgeTarget = 680;

        //the fault codes a faulty valve takes turns reporting
        private static readonly string[] FaultCodes = ["StuckValve", "LowBattery", "SensorDrift"];

        private readonly SensorCategory _category;
        private readonly DeviceProfile _profile;

        // Where this device is in its pattern. Different per device so they do not all misbehave together.
        private readonly int _phaseOffset;
        private int _tick;

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
            : this(category, seed, DeviceProfile.Healthy)
        {
        }

        public ReadingGenerator(SensorCategory category, int seed, DeviceProfile profile)
        {
            _category = category;
            _profile = profile;
            _random = new Random(seed);

            _baseRssi = -50 - _random.Next(36);
            _hopCount = 1 + _random.Next(3);
            _protocol = Protocols[seed % Protocols.Length];
            _phaseOffset = _random.Next(1000);

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
            _tick++;

            var reading = new DeviceReading
            {
                MacAddress = macAddress,
                RecordedAt = recordedAt,
                Protocol = _protocol
            };

            ApplyRadio(reading);

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
                    reading.Fault = NextFault();
                    break;
            }

            return reading;
        }

        //..............................................................................//

        //checks whether this device is in its quiet spell, given how many seconds the run has been going
        public bool IsQuietAt(double secondsRunning)
        {
            if (_profile != DeviceProfile.Intermittent)
            {
                return false;
            }

            var position = (secondsRunning + _phaseOffset) % QuietCycleSeconds;
            return position >= QuietStartSeconds && position < QuietStartSeconds + QuietLengthSeconds;
        }

        //..............................................................................//

        //works out where this device is in a pattern that repeats every so many readings
        private int Phase(int cycle)
        {
            return (_tick + _phaseOffset) % cycle;
        }

        //..............................................................................//

        //fills in the radio fields, which the weak and flaky devices bend out of shape
        private void ApplyRadio(DeviceReading reading)
        {
            var rssi = _baseRssi + _random.Next(-3, 4);
            var hops = _hopCount;
            var duty = _random.NextDouble() * 4;
            var loss = _random.NextDouble() * 3;

            if (_profile == DeviceProfile.WeakRadio)
            {
                // Depth is how far into the fade the device is, 0 at rest and 1 at the bottom.
                var depth = WeakDepth();

                rssi = -72 - (int)(depth * 24) + _random.Next(-2, 3);
                hops = depth > 0.5 ? 4 : 3;
                duty += depth * 6;
                loss += depth * 9;
            }
            else if (_profile == DeviceProfile.Flaky)
            {
                var burst = Phase(FlakyCycle);

                if (burst >= 10 && burst < 20)
                {
                    rssi += _random.Next(-12, 1);
                    duty = 8 + (_random.NextDouble() * 7);
                    loss = 8 + (_random.NextDouble() * 17);
                }
            }

            reading.Rssi = rssi;
            reading.HopCount = hops;
            reading.DutyCycle = Math.Round(duty, 1);
            reading.PacketLoss = Math.Round(loss, 1);
        }

        //..............................................................................//

        //works out how faded a weak radio is, from nothing up to the bottom and back
        private double WeakDepth()
        {
            var position = Phase(WeakCycle);

            if (position >= 30 && position < 60)
            {
                return (position - 30) / 30.0;
            }

            if (position >= 60 && position < 80)
            {
                return 1;
            }

            if (position >= 80 && position < 100)
            {
                return 1 - ((position - 80) / 20.0);
            }

            return 0;
        }

        //..............................................................................//

        //gives the fault code a faulty valve is reporting right now, or null when it is well
        private string? NextFault()
        {
            if (_profile != DeviceProfile.FaultyValve)
            {
                return null;
            }

            var position = Phase(FaultCycle);

            if (position < 20 || position >= 40)
            {
                return null;
            }

            // A different code each time round, so the faults do not all look the same.
            return FaultCodes[((_tick + _phaseOffset) / FaultCycle) % FaultCodes.Length];
        }

        //..............................................................................//

        //produces the next soil moisture percentage
        private double NextMoisture()
        {
            var drying = Phase(ThirstyCycle);

            if (_profile == DeviceProfile.Thirsty && drying >= 15 && drying < 45)
            {
                // Dries steadily to well under the gateway's fifteen percent.
                _value -= 1.4 + Wobble(0.4);
            }
            else if (_profile == DeviceProfile.Thirsty && drying >= 45 && drying < 55)
            {
                // Watered. Climbs back to the top of the band.
                _value = Math.Min(_value + 5, MaxMoisture);
            }
            else if (IsSpike())
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
            var surging = Phase(SurgeCycle);

            if (_profile == DeviceProfile.Surging && surging >= 20 && surging < 46)
            {
                // Closes half the gap to the target each reading, so it climbs fast and then hovers.
                _value += ((WattSurgeTarget - _value) * 0.5) + Wobble(15);
            }
            else if (IsSpike())
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
