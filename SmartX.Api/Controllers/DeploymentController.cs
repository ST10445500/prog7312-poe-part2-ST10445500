using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DeploymentController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    //handles checking a deployment hierarchy before the gateway accepts it
    [ApiController]
    [Route("api/deployment")]
    public class DeploymentController : ControllerBase
    {
        //what the gateway says when the body is not a tree it can read
        private const string UnreadableTree = "The deployment tree could not be read. It is either missing, malformed, or nested far deeper than a real deployment would be.";

        private static readonly JsonSerializerOptions TreeJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            MaxDepth = 256
        };

        private readonly DeploymentTreeValidator _validator;
        private readonly DeploymentStore _store;
        private readonly SensorStore _sensors;

        public DeploymentController(DeploymentTreeValidator validator, DeploymentStore store, SensorStore sensors)
        {
            _validator = validator;
            _store = store;
            _sensors = sensors;
        }

        //..............................................................................//

        //retrieves the deployment tree the gateway is running
        [HttpGet]
        public ActionResult<DeploymentNode> Get()
        {
            return _store.Get();
        }

        //..............................................................................//

        //replaces the deployment tree, refusing one the validator turns down
        [HttpPut]
        public async Task<ActionResult<DeploymentValidationResult>> Replace()
        {
            var root = await ReadTreeAsync();

            if (root == null)
            {
                return BadRequest(UnreadableTree);
            }

            var result = _validator.Validate(root);

            // The client checks first, but only for feedback. Nothing stops another caller.
            if (!result.IsValid)
            {
                return BadRequest(result);
            }

            _store.Replace(root);

            // The tree is the answer now, so registrations are brought back in line.
            _store.ApplyLocationsTo(_sensors);

            return result;
        }

        //..............................................................................//

        //validates a deployment tree such as zone, sub-zone, sensor
        [HttpPost("validate")]
        public async Task<ActionResult<DeploymentValidationResult>> Validate()
        {
            var root = await ReadTreeAsync();

            if (root == null)
            {
                return BadRequest(UnreadableTree);
            }

            return _validator.Validate(root);
        }

        //..............................................................................//

        //reads a deployment tree from the request body, or null
        private async Task<DeploymentNode?> ReadTreeAsync()
        {
            // Model binding gives up on a deeply nested body and 500s before the validator runs.
            string body;

            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<DeploymentNode>(body, TreeJsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
