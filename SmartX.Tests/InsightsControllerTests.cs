using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Collections;
using SmartX.Api.Controllers;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//InsightsControllerTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks that the insights route answers, turns away blank search text and learns from a valid one
    public class InsightsControllerTests
    {
        private static InsightsController Build()
        {
            var sensors = new SensorStore();
            DemoFleet.Seed(sensors);
            return new InsightsController(new InsightService(sensors, new ActiveFaults()));
        }

        [Fact]
        public void Get_FreshStart_ReturnsAnEmptyList()
        {
            // Arrange
            var controller = Build();

            // Act
            var result = controller.Get();

            // Assert
            Assert.Empty(result.Value!);
        }

        [Fact]
        public void Search_BlankText_ReturnsBadRequest()
        {
            // Arrange
            var controller = Build();

            // Act
            var result = controller.Search(new SearchRecord { Text = "  " });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Search_ValidText_ReturnsOkAndShowsInTheSuggestions()
        {
            // Arrange
            var controller = Build();

            // Act
            var result = controller.Search(new SearchRecord { Text = "AA:BB:CC:DD:EE:05" });
            var suggestions = controller.Get().Value!;

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.Equal("Open valve-b1", suggestions[0].Text);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
