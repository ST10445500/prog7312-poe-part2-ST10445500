using SmartX.Api.Collections;

//ST10445500 - PROG7312 - SmartX POE
//TransitionModelTests

//.....................................o0oSTART OF FILEo0o........................................//

// The unit tests check the data structures without needing the API to be running.

namespace SmartX.Tests
{
    //checks the counts, the ranking and tie order, the confidence, an empty model, bad names and that it holds up under many threads
    public class TransitionModelTests
    {

        [Fact]
        public void GetNext_OnePair_HasFullConfidence()
        {
            // Arrange
            var model = new TransitionModel();
            model.Record("a", "b");

            // Act
            var next = model.GetNext("a", 3);

            // Assert
            Assert.Single(next);
            Assert.Equal("b", next[0].Event);
            Assert.Equal(1, next[0].Count);
            Assert.Equal(1.0, next[0].Confidence);
        }

        [Fact]
        public void GetNext_Mixed_RanksMostOftenFirst()
        {
            // Arrange
            var model = new TransitionModel();
            model.Record("a", "x");
            model.Record("a", "y");
            model.Record("a", "y");
            model.Record("a", "y");
            model.Record("a", "z");
            model.Record("a", "z");

            // Act
            var next = model.GetNext("a", 3);

            // Assert
            Assert.Equal(new[] { "y", "z", "x" }, next.Select(p => p.Event));
        }

        [Fact]
        public void GetNext_Tied_OrdersByName()
        {
            // Arrange
            var model = new TransitionModel();
            model.Record("a", "zeta");
            model.Record("a", "alpha");
            model.Record("a", "mid");

            // Act
            var next = model.GetNext("a", 3);

            // Assert
            Assert.Equal(new[] { "alpha", "mid", "zeta" }, next.Select(p => p.Event));
        }

        [Fact]
        public void GetNext_Confidence_IsShareOfAllFollowers()
        {
            // Arrange
            var model = new TransitionModel();
            model.Record("a", "x");
            model.Record("a", "x");
            model.Record("a", "x");
            model.Record("a", "y");

            // Act
            var next = model.GetNext("a", 5);

            // Assert
            Assert.Equal(0.75, next[0].Confidence);
            Assert.Equal(0.25, next[1].Confidence);
            Assert.Equal(1.0, next.Sum(p => p.Confidence));
        }

        [Fact]
        public void Observe_InOrder_CountsEachNeighbourPair()
        {
            // Arrange
            var model = new TransitionModel();

            // Act
            model.Observe("a");
            model.Observe("b");
            model.Observe("a");
            model.Observe("b");

            // Assert
            Assert.Equal(2, model.GetNext("a", 1)[0].Count);
            Assert.Equal("b", model.GetNext("a", 1)[0].Event);
            Assert.Equal(1, model.GetNext("b", 1)[0].Count);
            Assert.Equal("b", model.Last);
        }

        [Fact]
        public void Record_ManyThreads_LosesNoCounts()
        {
            // Arrange
            var model = new TransitionModel();

            // Act
            Parallel.For(0, 5000, index => model.Record("a", index % 2 == 0 ? "x" : "y"));

            // Assert
            Assert.Equal(5000, model.Total("a"));
            Assert.Equal(2500, model.GetNext("a", 1)[0].Count);
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
