using System;

namespace Fiffi
{
    public record EventMetaData
	(
		Guid CorrelationId,
		Guid CausationId,
		Guid EventId,
		string StreamName,
		string AggregateName,
		long BasedOnStreamVersion,
		string TriggeredBy,
		long OccuredAt);
}
