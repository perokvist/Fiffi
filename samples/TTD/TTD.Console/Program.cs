using System;

namespace TTD.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var (time, _) = TTD.Fiffied.App.RunAsync(args).GetAwaiter().GetResult();
        }
    }
}
