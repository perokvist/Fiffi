using Fiffi;
using System.Collections.Generic;

namespace TTD.Fiffied
{
    public record TimePassed : EventRecord
    {
        public int Time { get; internal set; }
    }
}