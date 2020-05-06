using System;

namespace TTD
{
    class Program
    {
        static void Main(string[] args)
        {
            var (time, _) = TTD.Domain.Main.Run(args);
        }
    }
}
