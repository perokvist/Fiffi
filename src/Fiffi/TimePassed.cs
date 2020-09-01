using System.Collections.Generic;

namespace Fiffi
{
    public class TimePassed : IEvent
    {
        public string SourceId { get; set; }
        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

        public static TimePassed Raise(string sourceId)
        {
            var e = new TimePassed();
            e.SourceId = sourceId;
            e.Meta.AddMetaData();
            e.Meta.AddTypeInfo(e);
            return e;
        }
    }
}
