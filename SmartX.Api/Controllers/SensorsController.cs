using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//SensorsController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/sensors")]
    public class SensorsController : ControllerBase
    {
        private readonly SensorStore _store;
        private readonly DeploymentStore _deployment;

        public SensorsController(SensorStore store, DeploymentStore deployment)
        {
            _store = store;
            _deployment = deployment;
        }

        //..............................................................................//

        //registers a new sensor, or 409 if the mac address is already taken
        [HttpPost]
        public ActionResult<SensorRegistration> Register(SensorRegistration sensor)
        {
            if (!_store.TryRegister(sensor))
            {
                return Conflict($"A sensor with mac address {sensor.MacAddress} is already registered.");
            }

            // A registration with a zone and room is placed now. Name only waits to be dragged in.
            if (!string.IsNullOrWhiteSpace(sensor.Location.Zone) && !string.IsNullOrWhiteSpace(sensor.Location.Room))
            {
                _deployment.Place(sensor);
            }

            return CreatedAtAction(nameof(GetByMacAddress), new { macAddress = sensor.MacAddress }, sensor);
        }

        //..............................................................................//

        //retrieves every sensor registered with the gateway
        [HttpGet]
        public ActionResult<List<SensorRegistration>> GetAll()
        {
            return _store.GetAll();
        }

        //..............................................................................//

        //retrieves a sensor by mac address, or 404 if it is not registered
        [HttpGet("{macAddress}")]
        public ActionResult<SensorRegistration> GetByMacAddress(string macAddress)
        {
            var sensor = _store.Find(macAddress);

            if (sensor == null)
            {
                return NotFound();
            }

            return sensor;
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
