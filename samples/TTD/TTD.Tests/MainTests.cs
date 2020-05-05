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

        [Fact]
        public void Scenario_B_Time()
        {
            var (time, _) = Main.Run("B");

            Assert.Equal(5, time);
        }

        [Fact]
        public void Scenario_B_Events()
        {
            var (_, events) = Main.Run("B");

            Assert.Equal(2, events.Length);
            Assert.True(events.First().EventName == EventType.DEPART);
            Assert.True(events.Last().EventName == EventType.ARRVIVE);

        }

    }
}
