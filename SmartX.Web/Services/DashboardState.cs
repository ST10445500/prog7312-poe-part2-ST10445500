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
        private readonly HubConnection _hub;

        private string _searchText = string.Empty;
        private SensorCategory? _category;
        private bool _alertsOnly;
        private int _historyMinutes;

        public DashboardState(HttpClient http)
        {
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
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                // The api is down or the origin was refused. The page keeps polling and the next visit tries again.
                Console.WriteLine($"Could not open the stream connection: {ex.Message}");
            }
        }

        //..............................................................................//

        public async ValueTask DisposeAsync()
        {
            await _hub.DisposeAsync();
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
