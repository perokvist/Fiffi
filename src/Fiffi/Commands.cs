using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi;

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

    public static void GuaranteeCorrelation(this ICommand cmd)
    {
        if (cmd.CorrelationId == default)
            cmd.CorrelationId = Guid.NewGuid();

        if (cmd.CausationId == default)
            cmd.CausationId = Guid.NewGuid();
    }

    public static Task Dispatch(this IEnumerable<ICommand> commands, IEvent trigger, Func<IEvent, ICommand, Task> dispatcher)
        => Task.WhenAll(commands.Select(c => dispatcher(trigger, c)));
}
