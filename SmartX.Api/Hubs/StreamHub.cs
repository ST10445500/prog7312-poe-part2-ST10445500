using Microsoft.AspNetCore.SignalR;

//ST10445500 - PROG7312 - SmartX POE
//StreamHub

//.....................................o0oSTART OF FILEo0o........................................//

// The hub is the connection the command stream page listens on for changes.

namespace SmartX.Api.Hubs
{
    //has no methods of its own, the processor sends StreamChanged to everyone connected
    public class StreamHub : Hub
    {
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
