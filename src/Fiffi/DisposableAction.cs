using System;

namespace Fiffi
{
	public class DisposableAction : IDisposable
	{
		private readonly Action _a;

		public DisposableAction(Action a)
		{
			_a = a;
		}

		public void Dispose()
		{
			_a();
		}
	}
}