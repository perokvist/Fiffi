using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class DispatcherTests
    {
        [Fact]
        public async Task GuaranteeCorrelationAsync()
        {
            var d = new Dispatcher<ICommand, Task>();
            Guid c1 = default;
            Guid c2 = default;

            d.Register(Commands.GuaranteeCorrelation<TestCommand>(),
              cmd =>
              {
                  c1 = cmd.CorrelationId;
                  c2 = cmd.CausationId;
                  return Task.CompletedTask;
              });

            await d.Dispatch(new TestCommand("t"));

            Assert.True(c1 != default);
            Assert.True(c2 != default);
        }

        [Fact]
        public async Task ValidationShouldThrowAsync()
        {
            var d = new Dispatcher<ICommand, Task>();

            d.Register(Commands.Validate<TestCommand>());

            await Assert.ThrowsAsync<ValidationException>(() =>
            d.Dispatch(new TestCommand("t") { CorrelationId = default }));
        }

        [Fact]
        public async Task OrderingAsync()
        {
            var order = new List<int>();
            var d = new Dispatcher<ICommand, Task>();

            d.Register<TestCommand>(cmd => {
                order.Add(1);
                return Task.CompletedTask;
            },
            cmd => {
                order.Add(2);
                return Task.CompletedTask;
            }
            );

            await d.Dispatch(new TestCommand("t"));

            Assert.Equal(new [] { 1, 2 }, order);
        }


    }
}
