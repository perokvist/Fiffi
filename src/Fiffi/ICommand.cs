using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi
{
	public interface ICommand
	{
		IAggregateId AggregateId { get; }

		Guid CorrelationId { get; set; }

        Guid CausationId { get; set; }
    }
}
