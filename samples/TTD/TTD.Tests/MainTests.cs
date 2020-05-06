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

            Assert.Equal(3, events.Length);
            Assert.True(events.First().EventName == EventType.DEPART);
            Assert.True(events.Skip(1).First().EventName == EventType.ARRIVE);
        }

        [Fact] //TODO inline data
        public void Scenario_AB_Time()
        {
            var (time, _) = Main.Run("A", "B");

            Assert.Equal(5, time);
        }

        [Fact]
        public void Scenario_AB_Events()
        {
            var (_, events) = Main.Run("A", "B");

            Assert.Equal(10, events.Length);
            Assert.True(events.First().EventName == EventType.DEPART);
            Assert.True(events.Last().EventName == EventType.DEPART);
        }

    }
}
