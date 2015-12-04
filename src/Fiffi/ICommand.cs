using System;

namespace Fiffi
{
	public interface ICommand
	{
		Guid AggregateId { get; set; }
		Guid CorrelationId { get; set; }

	}
}