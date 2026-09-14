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

// Past the ring buffer's 120 and past the batch grid's 1000. Both get exercised.
const int BackfillReadings = 1200;

// Windows are ten seconds wide. Spread these wider and most of them come out empty.
const int BackfillIntervalSeconds = 3;

// About a reading a second. Slower than this and the dashboard looks frozen.
const double MinIntervalSeconds = 1;
const double MaxIntervalSeconds = 2;

var baseUrl = args.Length > 0 ? args[0] : DefaultBaseUrl;
var gateway = new GatewayClient(baseUrl);

Console.WriteLine($"Smart-X simulator, talking to {baseUrl}");

List<SensorRegistration> fleet;

try
{
	fleet = await gateway.GetFleetAsync();
}
catch (HttpRequestException problem)
{
	// Usually the api is not up, which the socket error does not say.
	Console.WriteLine($"Could not reach the gateway at {baseUrl}. Is SmartX.Api running?");
	Console.WriteLine(problem.Message);
	return;
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

for (var index = 0; index < reporting.Count; index++)
{
	generators[reporting[index].MacAddress] = new ReadingGenerator(reporting[index].Category, index + 1);
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
		batch.Add(generator.Next(sensor.MacAddress, recordedAt));

		if (batch.Count < GatewayClient.MaxBatchSize && index < BackfillReadings - 1)
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

// A task each, so readings really do land at once and the locks get exercised.
var streams = reporting
	.Select(sensor => StreamSensorAsync(sensor, stopping.Token))
	.ToList();

await Task.WhenAll(streams);

Console.WriteLine("Simulator stopped.");

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
			await gateway.PostReadingAsync(generator.Next(sensor.MacAddress, DateTime.UtcNow), token);
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
