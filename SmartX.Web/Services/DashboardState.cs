using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DashboardState

//.....................................o0oSTART OF FILEo0o........................................//

// The state lives in one scoped service so it survives a page change inside the client.

namespace SmartX.Web.Services
{
    //keeps what the command stream page needs to remember between visits, and the live connection to the api
    public class DashboardState : IAsyncDisposable
    {
        // How long the box has to sit still before the search counts as settled. Hard coded for now.
        private const int SearchSettleMilliseconds = 1500;

        private readonly HubConnection _hub;
        private readonly HttpClient _http;

        private CancellationTokenSource? _searchDelay;
        private string _lastPostedSearch = string.Empty;

        private string _searchText = string.Empty;
        private SensorCategory? _category;
        private bool _alertsOnly;
        private int _historyMinutes;

        public DashboardState(HttpClient http)
        {
            _http = http;

            // The hub sits on the same api the page already talks to.
            var hubUrl = new Uri(http.BaseAddress!, "hubs/stream");

            _hub = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hub.On("StreamChanged", () => OnStreamChanged?.Invoke());

            // Anything missed while the link was down is picked up by asking again.
            _hub.Reconnected += _ =>
            {
                OnStreamChanged?.Invoke();
                return Task.CompletedTask;
            };
        }

        //raised when anything held here changes
        public event Action? OnChange;

        //raised when the api says the stream has moved on, or the link came back
        public event Action? OnStreamChanged;

        //..............................................................................//

        //retrieves whether the live connection to the api is up
        public bool Connected => _hub.State == HubConnectionState.Connected;

        //..............................................................................//

        //retrieves or sets the text in the device search box
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnChange?.Invoke();

                _ = PostWhenSettledAsync(value);
            }
        }

        //..............................................................................//

        //waits for the search box to stop changing, then tells the api what was searched for
        private async Task PostWhenSettledAsync(string text)
        {
            // Typing again cancels the wait, so only the text that was left alone gets posted.
            _searchDelay?.Cancel();
            _searchDelay?.Dispose();

            var delay = new CancellationTokenSource();
            _searchDelay = delay;

            try
            {
                await Task.Delay(SearchSettleMilliseconds, delay.Token);

                var settled = text.Trim();

                if (settled.Length == 0 || settled == _lastPostedSearch)
                {
                    return;
                }

                var response = await _http.PostAsJsonAsync("api/insights/search", new SearchRecord { Text = settled }, ApiJsonOptions.Default, delay.Token);

                if (response.IsSuccessStatusCode)
                {
                    _lastPostedSearch = settled;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpRequestException ex)
            {
                // The engine only misses one search, the page carries on.
                Console.WriteLine($"Could not record the search: {ex.Message}");
            }
        }

        //..............................................................................//

        //retrieves or sets the category the list is narrowed to, null for all of them
        public SensorCategory? Category
        {
            get => _category;
            set
            {
                if (_category == value)
                {
                    return;
                }

                _category = value;
                OnChange?.Invoke();
            }
        }

        //..............................................................................//

        //retrieves or sets whether only devices with a fault or gone quiet are listed
        public bool AlertsOnly
        {
            get => _alertsOnly;
            set
            {
                if (_alertsOnly == value)
                {
                    return;
                }

                _alertsOnly = value;
                OnChange?.Invoke();
            }
        }

        //..............................................................................//

        //retrieves or sets how many minutes back the history card draws, 0 for everything kept
        public int HistoryMinutes
        {
            get => _historyMinutes;
            set
            {
                if (_historyMinutes == value)
                {
                    return;
                }

                _historyMinutes = value;
                OnChange?.Invoke();
            }
        }

        //..............................................................................//

        //opens the connection if it is not already open, failing quietly because the page polls anyway
        public async Task StartAsync()
        {
            if (_hub.State != HubConnectionState.Disconnected)
            {
                return;
            }

            try
            {
                await _hub.StartAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Broad on purpose, what a failed connect throws depends on the transport it fell back to.
                // The api is down or the origin was refused. The page keeps polling and tries again every tick.
                Console.WriteLine($"Could not open the stream connection: {ex.GetType().Name}: {ex.Message}");
            }
        }

        //..............................................................................//

        public async ValueTask DisposeAsync()
        {
            _searchDelay?.Cancel();
            _searchDelay?.Dispose();

            await _hub.DisposeAsync();
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
