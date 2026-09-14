using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//GatewayClient

//.....................................o0oSTART OF FILEo0o........................................//

// The client talks to the gateway over http, the same way a real device would.

namespace SmartX.Simulator
{
    //sends readings up and reads back the fleet to report for
    public class GatewayClient
    {
        //how many readings the gateway accepts in a single batch
        public const int MaxBatchSize = 500;

        // Categories come back as strings. Nothing is shared, so this says it again.
        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

        private readonly HttpClient _http;

        public GatewayClient(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        //..............................................................................//

        //retrieves every sensor the gateway has registered
        public async Task<List<SensorRegistration>> GetFleetAsync()
        {
            var fleet = await _http.GetFromJsonAsync<List<SensorRegistration>>("api/sensors", JsonOptions);
            return fleet ?? new List<SensorRegistration>();
        }

        //..............................................................................//

        //posts a batch of readings and reports what the gateway kept
        public async Task<BatchResult> PostBatchAsync(List<DeviceReading> readings)
        {
            var response = await _http.PostAsJsonAsync("api/telemetry/batch", readings, JsonOptions);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BatchResult>(JsonOptions);
            return result ?? new BatchResult();
        }

        //..............................................................................//

        //posts one reading, the way a device reports as it takes them
        public async Task PostReadingAsync(DeviceReading reading, CancellationToken token)
        {
            var response = await _http.PostAsJsonAsync("api/telemetry", reading, JsonOptions, token);
            response.EnsureSuccessStatusCode();
        }

        //..............................................................................//

        //builds the json settings every call from this client uses
        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
