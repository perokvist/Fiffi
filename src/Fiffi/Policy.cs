using System;

namespace Fiffi
{
    public static class Policy
    {
        public static T Issue<T>(IEvent @event, Func<T> f)
            where T : ICommand
        {
            var cmd = f();
            var meta = @event.Meta.GetEventMetaData();
            cmd.CorrelationId = meta.CorrelationId;
            cmd.CausationId = meta.EventId;
            return cmd;
        }
    }
}
