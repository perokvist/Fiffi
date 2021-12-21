namespace Fiffi;
public static partial class ApplicationService
{
    public static Action<IEnumerable<IEvent>> None(ICommand command)
    => events => { };

    public static Action<IEnumerable<IEvent>> ThrowOnCausation(ICommand command)
     => events =>
     {
         if (events.Any(e => e.GetCausationId() == command.CausationId))
             throw new Exception($"Duplicate Execution of command based on causation - ({command.CausationId}) - {command.GetType()}. Events with the same causation already exsist.");
     };
}
