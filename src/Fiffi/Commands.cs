using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class Commands
    {
        public static Func<T, Task> GuaranteeCorrelation<T>()
            where T : ICommand
            => cmd =>
            {
                cmd.GuaranteeCorrelation();
                return Task.CompletedTask;
            };

        public static Func<T, Task> Validate<T>()
            where T : ICommand
            => cmd =>
            {
                Validator.ValidateObject(cmd, new ValidationContext(cmd), true);
                return Task.CompletedTask;
            };

        public static void GuaranteeCorrelation(this ICommand cmd) =>
            Guid.NewGuid()
            .DoIf(x => x != default,
                id => id
                .Tap(x => cmd.CorrelationId = x)
                .Tap(x => cmd.CausationId = x));

    }
}
