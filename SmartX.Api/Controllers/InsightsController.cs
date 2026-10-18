using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//InsightsController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    //hands the page what the engine suggests and takes the searches it should learn from
    [ApiController]
    [Route("api/insights")]
    public class InsightsController : ControllerBase
    {
        private readonly InsightService _insights;

        public InsightsController(InsightService insights)
        {
            _insights = insights;
        }

        //..............................................................................//

        //retrieves the devices that need attention and what has usually followed the last thing that happened
        [HttpGet]
        public ActionResult<List<Suggestion>> Get()
        {
            return _insights.GetSuggestions();
        }

        //..............................................................................//

        //records a search the page has settled on
        [HttpPost("search")]
        public ActionResult Search(SearchRecord request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("The search text cannot be empty.");
            }

            _insights.RecordSearch(request.Text);

            return Ok();
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
