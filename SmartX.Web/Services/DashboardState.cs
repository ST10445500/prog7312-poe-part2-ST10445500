//ST10445500 - PROG7312 - SmartX POE
//DashboardState

//.....................................o0oSTART OF FILEo0o........................................//

// The state lives in one scoped service so it survives a page change inside the client.

namespace SmartX.Web.Services
{
    //keeps what the command stream page needs to remember between visits
    public class DashboardState
    {
        private string _searchText = string.Empty;

        //raised when anything held here changes
        public event Action? OnChange;

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
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
