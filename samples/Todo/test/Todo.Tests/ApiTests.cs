using System;
using Fiffi.Testing;
using Todo.Todo;
using Xunit;

namespace Todo.Tests
{
	public class ApiTests : IClassFixture<TestContext>
	{
		private readonly TestContext _context;

		public ApiTests(TestContext context)
		{
			_context = context;
			TodoModule.IdGenerator = () => Guid.Empty;
		}

		[Fact]
		public async void task_created() =>
			await _context.RunAsync(Env.UseCase(
				"testing", 
				when: Spec.GetJSON("http://localhost/api/"),
				thenResponse: new { Name = "Test", id = TodoModule.IdGenerator()},
				thenEvents: Spec.Events(new TaskCreated()
				{
					Name = "Test",
					AggregateId = TodoModule.IdGenerator()
				})));
	}
}