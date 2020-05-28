using Fiffi;
using System.Collections.Generic;

namespace TTD.Domain.Fiffied
{
    public class TimePassed : IEvent
    {
        public int Time { get; internal set; }
        public string SourceId => Time.ToString();
        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    }
}