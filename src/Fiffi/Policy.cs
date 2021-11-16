using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi;

public static class Policy
{
    public static T[] Issue<T>(IEvent @event, Func<T[]> f)
    where T : ICommand
        => f()
        .Select(x => Issue(@event, () => x))
        .ToArray();

    public static T Issue<T>(IEvent @event, Func<T> f)
        where T : ICommand
    {
        var cmd = f();
        if (cmd != null)
        {
            //cmd.CommandId = $"{@event.EventId()}-{cmd.GetType()}-{commandIndex}";
            cmd.CorrelationId = @event.GetCorrelation();
            cmd.CausationId = @event.EventId();
        }
        return cmd;
    }

    public static async Task<T[]> Issue<T>(IEvent @event, Func<Task<T[]>> f)
    where T : ICommand
    {
        var cmds = await f();
        return Issue(@event, () => cmds);
    }

    public static async Task<T> Issue<T>(IEvent @event, Func<Task<T>> f)
       where T : ICommand
    {
        var cmd = await f();
        return Issue(@event, () => cmd);
    }
}
