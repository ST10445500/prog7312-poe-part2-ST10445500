using SmartX.Shared.Models;
using SmartX.Simulator;

//ST10445500 - PROG7312 - SmartX POE
//Program

//.....................................o0oSTART OF FILEo0o........................................//

// Program starts the simulator and reports for whatever the gateway has registered.

// Same address the blazor client is pointed at.
const string DefaultBaseUrl = "https://localhost:7002/";

// Registered but never powered up. Gives the dashboard something silent to show.
const string SpareNodeId = "spare-valve";

// Past the ring buffer's 300 and past the batch grid's 1000. Both get exercised.
const int BackfillReadings = 1200;

// Windows are ten seconds wide. Spread these wider and most of them come out empty.
const int BackfillIntervalSeconds = 3;

// About a reading a second. Slower than this and the dashboard looks frozen.
const double MinIntervalSeconds = 1;
const double MaxIntervalSeconds = 2;

// --drop goes quiet for a while after a short spell, so a silent node and its return can be shown.
const double DropAfterSeconds = 10;
const double DropForSeconds = 40;

// What a valve says when --fault is used.
const string FaultCode = "StuckValve";

// --critical waits a little, floods the gateway, then sends one dry reading from the named moisture sensor.
const double CriticalAfterSeconds = 5;
const int CriticalBurstReadings = 300;
const double CriticalMoisture = 5;

// Left to itself the simulator floods the gateway this often, so a queue that backs up is seen without a flag.
const double BurstEverySeconds = 75;

// About one live reading in this many is sent a second time, late or with the same timestamp. Not valves, a stale state would undo a command.
const int RepeatOdds = 20;
const double LateBySeconds = 8;

// How long it keeps asking for the gateway before giving up, for when both are started together.
const double GatewayWaitSeconds = 45;

var baseUrl = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal) ? args[0] : DefaultBaseUrl;
var faultMac = ReadFlag("--fault");
var dropMac = ReadFlag("--drop");
var criticalMac = ReadFlag("--critical");
var startedAt = DateTime.UtcNow;
var gateway = new GatewayClient(baseUrl);

Console.WriteLine($"Smart-X simulator, talking to {baseUrl}");

List<SensorRegistration> fleet;

// Started from Visual Studio next to the api, so the api may still be coming up. Only a refused connection is waited out.
var giveUpAt = DateTime.UtcNow.AddSeconds(GatewayWaitSeconds);
var waitingShown = false;

while (true)
{
	try
	{
		// The gateway only seeds nine, so the rest of the facility is registered from here.
		var added = 0;

		foreach (var extra in ExtraFleet.Build())
		{
			if (await gateway.RegisterAsync(extra))
			{
				added++;
			}
		}

		Console.WriteLine($"Registered {added} extra sensors.");
		fleet = await gateway.GetFleetAsync();
		break;
	}
	catch (HttpRequestException problem) when (problem.InnerException is System.Security.Authentication.AuthenticationException)
	{
		// The api is up but the secure connection failed, nearly always the dev certificate, which looks the same as it being down.
		Console.WriteLine($"Reached {baseUrl} but could not set up a secure connection. The https development certificate is usually not trusted.");
		Console.WriteLine("Run dotnet dev-certs https --trust and start the simulator again.");
		Console.WriteLine(problem.Message);
		return;
	}
	catch (HttpRequestException problem) when (problem.InnerException is System.Net.Sockets.SocketException && DateTime.UtcNow < giveUpAt)
	{
		if (!waitingShown)
		{
			Console.WriteLine("Waiting for the gateway to start...");
			waitingShown = true;
		}

		await Task.Delay(TimeSpan.FromSeconds(2));
	}
	catch (HttpRequestException problem)
	{
		// Usually the api is not up, which the socket error does not say.
		Console.WriteLine($"Could not reach the gateway at {baseUrl}. Is SmartX.Api running?");
		Console.WriteLine(problem.Message);
		return;
	}
}

var reporting = fleet
	.Where(sensor => sensor.Location.NodeId != SpareNodeId)
	.ToList();

if (reporting.Count == 0)
{
	Console.WriteLine("The gateway has no sensors to report for.");
	return;
}

Console.WriteLine($"Reporting for {reporting.Count} of {fleet.Count} registered sensors.");

// One generator per sensor, seeded differently so the fleet does not move in step.
var generators = new Dictionary<string, ReadingGenerator>();
var perCategory = new Dictionary<SensorCategory, int>();

for (var index = 0; index < reporting.Count; index++)
{
	// The nth device of a kind gets the nth profile for that kind, so each fleet misbehaves the same way every run.
	var category = reporting[index].Category;
	perCategory.TryGetValue(category, out var position);
	perCategory[category] = position + 1;

	generators[reporting[index].MacAddress] = new ReadingGenerator(category, index + 1, DeviceProfiles.For(category, position));
}

Console.WriteLine($"Backfilling {BackfillReadings} readings per sensor...");

var backfillStart = DateTime.UtcNow.AddSeconds(-BackfillReadings * BackfillIntervalSeconds);

foreach (var sensor in reporting)
{
	var generator = generators[sensor.MacAddress];
	var batch = new List<DeviceReading>();
	var accepted = 0;
	var rejected = 0;
	var firstReason = string.Empty;

	for (var index = 0; index < BackfillReadings; index++)
	{
		var recordedAt = backfillStart.AddSeconds(index * BackfillIntervalSeconds);

		// A quiet device leaves a hole in its history instead of a reading.
		if (!generator.IsQuietAt(index * BackfillIntervalSeconds))
		{
			batch.Add(generator.Next(sensor.MacAddress, recordedAt));
		}

		if (batch.Count < GatewayClient.MaxBatchSize && index < BackfillReadings - 1)
		{
			continue;
		}

		// The last window can land in a quiet spell with nothing in it.
		if (batch.Count == 0)
		{
			continue;
		}

		var result = await gateway.PostBatchAsync(batch);

		accepted += result.Accepted;
		rejected += result.Rejected.Count;

		if (firstReason.Length == 0 && result.Rejected.Count > 0)
		{
			firstReason = result.Rejected[0].Reason;
		}

		batch.Clear();
	}

	// A rejection means the generator built something the gateway will not take.
	var note = rejected > 0 ? $", {rejected} rejected - {firstReason}" : string.Empty;
	Console.WriteLine($"  {sensor.MacAddress}  {accepted} accepted{note}");
}

Console.WriteLine("Backfill done.");

// Ctrl-C asks the sensor tasks to stop, mid-post is not a good place to die.
using var stopping = new CancellationTokenSource();

Console.CancelKeyPress += (_, pressed) =>
{
	pressed.Cancel = true;
	stopping.Cancel();
};

Console.WriteLine($"Streaming live readings every {MinIntervalSeconds} to {MaxIntervalSeconds} seconds. Ctrl-C to stop.");

// The backfill takes a while, so the quiet spell is timed from here and not from launch.
startedAt = DateTime.UtcNow;

// A task each, so readings really do land at once and the locks get exercised.
var streams = reporting
	.Select(sensor => StreamSensorAsync(sensor, stopping.Token))
	.ToList();

if (criticalMac != null)
{
	streams.Add(SendCriticalAsync(criticalMac, stopping.Token));
}

streams.Add(SendScheduledBurstsAsync(stopping.Token));

await Task.WhenAll(streams);

Console.WriteLine("Simulator stopped.");

//..............................................................................//

//retrieves the text after a flag such as --fault, or null if the flag was not given
string? ReadFlag(string flag)
{
	var position = Array.IndexOf(args, flag);
	return position >= 0 && position + 1 < args.Length ? args[position + 1] : null;
}

//floods the gateway with ordinary readings and then sends one dry reading from the named sensor
async Task SendCriticalAsync(string macAddress, CancellationToken token)
{
	var target = reporting.FirstOrDefault(sensor => string.Equals(sensor.MacAddress, macAddress, StringComparison.OrdinalIgnoreCase));

	if (target == null || target.Category != SensorCategory.Environmental)
	{
		Console.WriteLine($"--critical needs a moisture sensor this simulator is reporting for, and {macAddress} is not one.");
		return;
	}

	try
	{
		await Task.Delay(TimeSpan.FromSeconds(CriticalAfterSeconds), token);

		await SendBurstAndDryReadingAsync(target, token);
		Console.WriteLine($"Sent {CriticalBurstReadings} readings and then a moisture of {CriticalMoisture}% from {target.MacAddress}.");
	}
	catch (OperationCanceledException)
	{
		return;
	}
	catch (HttpRequestException problem)
	{
		Console.WriteLine($"  --critical could not post: {problem.Message}");
	}
}

//..............................................................................//

//floods the gateway with ordinary readings and then sends one dry reading from the target
async Task SendBurstAndDryReadingAsync(SensorRegistration target, CancellationToken token)
{
	// Fresh generators, because the streaming tasks are still using the shared ones.
	var others = reporting.Where(sensor => sensor != target).ToList();
	var burstGenerators = others
		.Select((sensor, index) => new ReadingGenerator(sensor.Category, 1000 + index))
		.ToList();

	var burst = new List<Task>();

	for (var index = 0; index < CriticalBurstReadings; index++)
	{
		var position = index % others.Count;
		var reading = burstGenerators[position].Next(others[position].MacAddress, DateTime.UtcNow);
		burst.Add(gateway.PostReadingAsync(reading, token));
	}

	await Task.WhenAll(burst);

	await gateway.PostReadingAsync(new DeviceReading
	{
		MacAddress = target.MacAddress,
		Value = CriticalMoisture,
		RecordedAt = DateTime.UtcNow,
		Rssi = -60
	}, token);
}

//..............................................................................//

//repeats the flood and a dry reading on its own, one moisture sensor after another, so the alert jumping the queue is not a one-off
async Task SendScheduledBurstsAsync(CancellationToken token)
{
	var sensors = reporting.Where(sensor => sensor.Category == SensorCategory.Environmental).ToList();

	if (sensors.Count == 0)
	{
		return;
	}

	var next = 0;

	try
	{
		while (!token.IsCancellationRequested)
		{
			await Task.Delay(TimeSpan.FromSeconds(BurstEverySeconds), token);

			var target = sensors[next % sensors.Count];
			next++;

			await SendBurstAndDryReadingAsync(target, token);
			Console.WriteLine($"  Burst of {CriticalBurstReadings} readings, then a dry reading from {target.MacAddress}.");
		}
	}
	catch (OperationCanceledException)
	{
		return;
	}
	catch (HttpRequestException problem)
	{
		Console.WriteLine($"  Scheduled burst could not post: {problem.Message}");
	}
}

//..............................................................................//

//checks whether this sensor is inside its quiet spell
bool IsDropped(string macAddress)
{
	var running = (DateTime.UtcNow - startedAt).TotalSeconds;

	// A device whose profile is to wander off does so with or without the flag.
	if (generators[macAddress].IsQuietAt(running))
	{
		return true;
	}

	if (!string.Equals(macAddress, dropMac, StringComparison.OrdinalIgnoreCase))
	{
		return false;
	}

	return running >= DropAfterSeconds && running < DropAfterSeconds + DropForSeconds;
}

//..............................................................................//

//keeps one sensor reporting until the simulator is asked to stop
async Task StreamSensorAsync(SensorRegistration sensor, CancellationToken token)
{
	var generator = generators[sensor.MacAddress];
	var jitter = new Random(sensor.MacAddress.GetHashCode());

	while (!token.IsCancellationRequested)
	{
		// Different wait each time, or the fleet falls into one beat.
		var seconds = MinIntervalSeconds + (jitter.NextDouble() * (MaxIntervalSeconds - MinIntervalSeconds));

		try
		{
			await Task.Delay(TimeSpan.FromSeconds(seconds), token);

			if (IsDropped(sensor.MacAddress))
			{
				continue;
			}

			// A pump or valve asks for an override before it reports, so what it says matches what it was told.
			if (sensor.Category == SensorCategory.Actuator)
			{
				try
				{
					var pending = await gateway.GetPendingAsync(sensor.MacAddress, token);

					if (pending != null)
					{
						generator.SetState(pending.Value);
					}
				}
				catch (HttpRequestException problem)
				{
					// A failed ask must not silence the reading too, or the node looks dropped.
					Console.WriteLine($"  {sensor.MacAddress} could not check for commands: {problem.Message}");
				}
			}

			var reading = generator.Next(sensor.MacAddress, DateTime.UtcNow);

			if (string.Equals(sensor.MacAddress, faultMac, StringComparison.OrdinalIgnoreCase))
			{
				reading.Fault = FaultCode;
			}

			await gateway.PostReadingAsync(reading, token);

			// Radios drop and retry, so the same reading sometimes turns up again, or turns up late.
			if (sensor.Category != SensorCategory.Actuator && jitter.Next(RepeatOdds) == 0)
			{
				var again = new DeviceReading
				{
					MacAddress = reading.MacAddress,
					Value = reading.Value,
					State = reading.State,
					RecordedAt = jitter.Next(2) == 0 ? reading.RecordedAt : reading.RecordedAt?.AddSeconds(-LateBySeconds),
					Rssi = reading.Rssi,
					HopCount = reading.HopCount,
					DutyCycle = reading.DutyCycle,
					PacketLoss = reading.PacketLoss,
					Protocol = reading.Protocol,
					Fault = reading.Fault
				};

				await gateway.PostReadingAsync(again, token);
			}
		}
		catch (OperationCanceledException)
		{
			return;
		}
		catch (HttpRequestException problem)
		{
			// One device losing the gateway should not stop the others.
			Console.WriteLine($"  {sensor.MacAddress} could not post: {problem.Message}");
		}
	}
}

//.....................................o0oEND OF FILEo0o..........................................//
