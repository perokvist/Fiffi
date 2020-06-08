using System;

namespace Fiffi
{
    public class EventMetaData
	{
		public Guid CorrelationId { get; set; }
        public Guid CausationId { get; set; }
        public Guid EventId { get; set; }
		public string StreamName { get; set; }
		public string AggregateName { get; set; }
		public long BasedOnStreamVersion { get; set; }
		public string TriggeredBy { get; set; }
		public long OccuredAt { get; set; }
	}
}
