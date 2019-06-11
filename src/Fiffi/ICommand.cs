using Fiffi.Validation;
using System;

namespace Fiffi
{
    public interface ICommand
	{
		IAggregateId AggregateId { get; }

        Guid CorrelationId { get; set; }

        Guid CausationId { get; set; }
    }
}
