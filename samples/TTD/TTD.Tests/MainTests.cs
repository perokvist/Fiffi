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
        [InlineData(7, "A", "B", "B")]
        [InlineData(29, "A", "A", "B", "A", "B", "B", "A", "B")]

        public void Scenarios(int expectedTime, params string[] cargo)
        {
            var (time, _) = Main.Run(cargo);

            Assert.Equal(expectedTime, time);
        }

        [Theory]
        [InlineData(3, "B")]
        [InlineData(10, "A", "B")]
        [InlineData(13, "A", "B", "B")]
        public void Scenarios_Events(int expectedEventCount, params string[] cargo)
        {
            var (_, events) = Main.Run(cargo);

            Assert.Equal(expectedEventCount, events.Length);
        }

//        [Theory]
//        [InlineData("B")]
//        [InlineData("A", "B")]
//        public void Scenarios_CorrectDelivery(params string[] cargo)
//        {
//            var locations = new[]
//{
//                new CargoLocation
//                {
//                    Location = Location.Factory,
//                    Cargo = cargo.Select(x =>
//                        new Cargo(0, Location.Factory, (Location) Enum.Parse(typeof(Location) , x, true))
//                    ).ToArray()
//                }
//            };
//            var (_, events) = Main.Run(cargo);

//            Assert.True(events.GetCargoLocations(locations)
//                .AllDelivered(cargo.Select(x => (Location) Enum.Parse(typeof(Location), x, true)).ToArray()));
//        }

    }
}
