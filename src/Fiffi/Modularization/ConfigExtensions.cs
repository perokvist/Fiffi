namespace Fiffi.Modularization;
public static class ConfigExtensions
{
    public static Configuration<T> Triggers<T>(
        this Configuration<T> config,
        Func<IEvent, ICommand?> f)
        where T : Module
        => config.Triggers(e => Task.FromResult(f(e)));

    public static Configuration<T> Triggers<T>(
      this Configuration<T> config,
      Func<IEvent, Task<ICommand?>> f)
      where T : Module
      => config.Tap(x => x.Triggers(async (events, d) =>
      {
          foreach (var e in events)
          {
              ICommand cmd = await f(e);
              if (cmd != null)
                  await d(e, cmd);
          }
      }));

}
