//ST10445500 - PROG7312 - SmartX POE
//RadioStats

//.....................................o0oSTART OF FILEo0o........................................//

// This shape keeps API input and output simple instead of exposing internal types.

namespace SmartX.Shared.Models
{
    //holds what the radio link said about itself when a reading arrived
    //every field is optional, a device that does not report one leaves it null
    public class RadioStats
    {
        //signal strength in dBm, closer to zero is stronger
        public int? Rssi { get; set; }

        //how many hops the packet took to reach the gateway
        public int? HopCount { get; set; }

        //the share of airtime the radio has used, as a percentage
        public double? DutyCycle { get; set; }

        //the share of packets lost on the way, as a percentage
        public double? PacketLoss { get; set; }

        //the radio protocol in use, such as ESP-NOW, Zigbee or Thread
        public string? Protocol { get; set; }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
