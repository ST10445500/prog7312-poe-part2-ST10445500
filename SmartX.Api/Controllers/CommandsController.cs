using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//CommandsController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    //handles manual overrides sent to pumps and valves, and undoing them
    [ApiController]
    [Route("api/commands")]
    public class CommandsController : ControllerBase
    {
        private readonly SensorStore _sensors;
        private readonly CommandService _commands;

        public CommandsController(SensorStore sensors, CommandService commands)
        {
            _sensors = sensors;
            _commands = commands;
        }

        //..............................................................................//

        //opens or closes a pump or valve by hand
        [HttpPost]
        public ActionResult Send(PumpCommandRequest request)
        {
            var sensor = _sensors.Find(request.MacAddress ?? string.Empty);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {request.MacAddress}.");
            }

            if (sensor.Category != SensorCategory.Actuator)
            {
                return BadRequest($"{sensor.MacAddress} is not a pump or valve, so it cannot be switched.");
            }

            _commands.Send(sensor.MacAddress, request.Open);

            return Ok($"{sensor.MacAddress} {(request.Open ? "opened" : "closed")}.");
        }

        //..............................................................................//

        //closes every pump and valve that is open, as one override that a single undo puts back
        [HttpPost("close-all")]
        public ActionResult ShutAll()
        {
            var closed = _commands.ShutAll();

            if (closed == 0)
            {
                return Ok("No valves were open.");
            }

            return Ok($"{closed} {(closed == 1 ? "valve" : "valves")} closed.");
        }

        //..............................................................................//

        //hands a pump or valve the override waiting for it, which a device calls for before it reports
        [HttpGet("pending/{macAddress}")]
        public ActionResult<PumpCommandRequest> Pending(string macAddress)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            var open = _commands.TakePending(sensor.MacAddress);

            if (open == null)
            {
                return NoContent();
            }

            return Ok(new PumpCommandRequest { MacAddress = sensor.MacAddress, Open = open.Value });
        }

        //..............................................................................//

        //retrieves the overrides that can be undone, newest first
        [HttpGet("history")]
        public ActionResult<List<CommandSummary>> History()
        {
            return _commands.GetHistory();
        }

        //..............................................................................//

        //puts back the state the newest override replaced
        [HttpPost("undo")]
        public ActionResult Undo()
        {
            if (!_commands.TryUndo(out var undone) || undone == null)
            {
                return Conflict("There is nothing to undo.");
            }

            if (undone.Changes.Count == 1)
            {
                var change = undone.Changes[0];
                return Ok($"{change.MacAddress} put back to {(change.PreviousOpen ? "open" : "closed")}.");
            }

            return Ok($"Put back {undone.Changes.Count} devices.");
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
