using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi
{
	public interface ICommand
	{
		Guid AggregateId { get; }
	}
}
