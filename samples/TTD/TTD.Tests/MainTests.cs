﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTD.Domain;
using TTD.Domain.Fiffied;
using Xunit;

namespace TTD.Tests
{
    public class MainFiffiTests
    {
        [Theory]
        //[InlineData(5, "B")]
        //[InlineData(5, "A")]
        [InlineData(5, "A", "B")]
        //[InlineData(7, "A", "B", "B")]
        //[InlineData(29, "A", "A", "B", "A", "B", "B", "A", "B")]
        //[InlineData(29, "A", "A", "A", "A", "B", "B", "B", "B")]
        //[InlineData(49, "B", "B", "B", "B", "A", "A", "A", "A")]
        public async Task ScenariosAsync(int expectedTime, params string[] cargo)
        {
            var (time, events) = await MainFiffi.RunAsync(cargo);

            Assert.Equal(expectedTime, time);
        }

        //[Theory]
        //[InlineData(3, "B")]
        //[InlineData(10, "A", "B")]
        //[InlineData(13, "A", "B", "B")]
        //public void Scenarios_Events(int expectedEventCount, params string[] cargo)
        //{
        //    var (_, events) = Main.Run(cargo);

        //    Assert.Equal(expectedEventCount, events.Length);
        //}
    }
}
