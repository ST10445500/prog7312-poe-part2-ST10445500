using SmartX.Api.Collections;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//InsightService

//.....................................o0oSTART OF FILEo0o........................................//

// The service keeps business rules and validation away from the controller.

namespace SmartX.Api.Services
{
    //learns which alerts, overrides and searches follow which, and turns that into suggestions with a reason
    public class InsightService
    {
        private const int MaxSearchLength = 64;
        private const int MaxNextSteps = 3;

        private readonly TransitionModel _model = new();
        private readonly SensorStore _sensors;
        private readonly ActiveFaults _faults;

        public InsightService(SensorStore sensors, ActiveFaults faults)
        {
            _sensors = sensors;
            _faults = faults;
            Seed();
        }

        //..............................................................................//

        //notes that an alert was raised by a rule
        public void RecordAlert(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
            {
                return;
            }

            _model.Observe($"alert:{rule}");
        }

        //..............................................................................//

        //notes that a pump or valve was switched by hand
        public void RecordCommand(string macAddress, bool open)
        {
            _model.Observe($"command:{(open ? "open" : "close")}:{macAddress.ToUpperInvariant()}");
        }

        //..............................................................................//

        //notes that every open valve was shut at once
        public void RecordShutAll()
        {
            _model.Observe("command:shutall");
        }

        //..............................................................................//

        //notes that an override was undone
        public void RecordUndo()
        {
            _model.Observe("command:undo");
        }

        //..............................................................................//

        //notes a search, returning false if it was blank or the same as the one before
        public bool RecordSearch(string text)
        {
            var key = Clean(text);

            if (key.Length == 0)
            {
                return false;
            }

            // The page can post the same settled search twice, and that is not a new event.
            if (_model.Last == $"search:{key}")
            {
                return false;
            }

            _model.Observe($"search:{key}");
            return true;
        }

        //..............................................................................//

        //retrieves devices that need attention now, then what has usually followed the last thing that happened
        public List<Suggestion> GetSuggestions()
        {
            var suggestions = new List<Suggestion>();

            foreach (var (mac, code) in _faults.GetErrors())
            {
                suggestions.Add(new Suggestion
                {
                    Kind = "Problem",
                    Event = $"fault:{code}",
                    Text = $"Check {NameOf(mac)}",
                    Confidence = 1,
                    Reason = $"{NameOf(mac)} is reporting {code}.",
                    MacAddress = mac
                });
            }

            foreach (var mac in _faults.GetDisconnected())
            {
                suggestions.Add(new Suggestion
                {
                    Kind = "Problem",
                    Event = "fault:Silent",
                    Text = $"Check {NameOf(mac)}",
                    Confidence = 1,
                    Reason = $"{NameOf(mac)} has stopped reporting.",
                    MacAddress = mac
                });
            }

            var last = _model.Last;

            if (last == null)
            {
                return suggestions;
            }

            var total = _model.Total(last);

            foreach (var next in _model.GetNext(last, MaxNextSteps))
            {
                var (text, mac, open) = Describe(next.Event);

                suggestions.Add(new Suggestion
                {
                    Kind = "Next",
                    Event = next.Event,
                    Text = text,
                    Confidence = next.Confidence,
                    Reason = $"{Prefix(last)}, this came next {next.Count} of {total} {(total == 1 ? "time" : "times")}.",
                    MacAddress = mac,
                    Open = open
                });
            }

            return suggestions;
        }

        //..............................................................................//

        //gives the engine some history so it has something to suggest on a first run
        private void Seed()
        {
            const string pumpA = "AA:BB:CC:DD:EE:02";
            const string pumpB = "AA:BB:CC:DD:EE:06";
            const string soilB = "AA:BB:CC:DD:EE:05";

            // Record, not Observe, so nothing has happened yet and the seed does not become the last event.
            Seed("alert:LowMoisture", $"command:open:{pumpA}", 5);
            Seed("alert:LowMoisture", $"command:close:{pumpA}", 1);
            Seed("alert:HighWatts", $"command:close:{pumpB}", 3);
            Seed($"search:{soilB.ToLowerInvariant()}", $"command:open:{pumpB}", 3);
        }

        //..............................................................................//

        //records the same pair a number of times
        private void Seed(string from, string to, int times)
        {
            for (var count = 0; count < times; count++)
            {
                _model.Record(from, to);
            }
        }

        //..............................................................................//

        //trims a search, puts it in lower case and cuts it to a sensible length
        private static string Clean(string? text)
        {
            var key = (text ?? string.Empty).Trim().ToLowerInvariant();
            return key.Length > MaxSearchLength ? key[..MaxSearchLength] : key;
        }

        //..............................................................................//

        //retrieves the name a device is known by, or its mac address if it is not registered
        private string NameOf(string macAddress)
        {
            return _sensors.Find(macAddress)?.Location.NodeId ?? macAddress;
        }

        //..............................................................................//

        //says in words what an event is, with the device and direction if it switches one
        private (string Text, string? Mac, bool? Open) Describe(string key)
        {
            var parts = key.Split(':', 3);

            switch (parts[0])
            {
                case "alert":
                    return ($"Watch for a {key["alert:".Length..]} alert", null, null);
                case "search":
                    return ($"Search for {key["search:".Length..]}", null, null);
                case "command" when parts[1] == "open" || parts[1] == "close":
                    var open = parts[1] == "open";
                    return ($"{(open ? "Open" : "Close")} {NameOf(parts[2])}", parts[2], open);
                case "command" when parts[1] == "shutall":
                    return ("Shut all valves", null, null);
                case "command":
                    return ("Undo the last override", null, null);
                default:
                    return (key, null, null);
            }
        }

        //..............................................................................//

        //says in words what the earlier event was, such as After a LowMoisture alert
        private string Prefix(string key)
        {
            var parts = key.Split(':', 3);

            switch (parts[0])
            {
                case "alert":
                    return $"After a {key["alert:".Length..]} alert";
                case "search":
                    return $"After searching for {key["search:".Length..]}";
                case "command" when parts[1] == "open" || parts[1] == "close":
                    return $"After you {(parts[1] == "open" ? "opened" : "closed")} {NameOf(parts[2])}";
                case "command" when parts[1] == "shutall":
                    return "After shutting all valves";
                case "command":
                    return "After an undo";
                default:
                    return $"After {key}";
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
