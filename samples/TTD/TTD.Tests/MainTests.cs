using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TTD.Domain;
using Xunit;

namespace TTD.Tests
{
    public class MainTests
    {
        [Theory]
        [InlineData(5, "B")]
        [InlineData(5, "A")]
        [InlineData(5, "A", "B")]
        //[InlineData(7, "A", "B", "B")]
        public void Scenarios(int expectedTime, params string[] cargo)
        {
            var (time, _) = Main.Run(cargo);

            Assert.Equal(expectedTime, time);
        }

        [Theory]
        [InlineData(3, "B")]
        [InlineData(10, "A", "B")]
        public void Scenarios_Events(int expectedEventCount, params string[] cargo)
        {
            var (_, events) = Main.Run(cargo);

            Assert.Equal(expectedEventCount, events.Length);
        }
    }
}
