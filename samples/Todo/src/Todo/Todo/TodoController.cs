using System;
using System.Threading.Tasks;
using Fiffi;
using Microsoft.AspNet.Mvc;

namespace Todo.Todo
{
	public class TodoController : Controller
	{
		private readonly TodoModule _m;

		public TodoController(TodoModule m)
		{
			_m = m;
		}

		[Route("api/")]
		[HttpGet]
		public async Task<IActionResult> Get()
		{
			var e = new TaskCreated()
			{
				AggregateId = TodoModule.IdGenerator(),
				Name = "Test"
			};

			await _m.PublishAsync(e);

			return Json(new {e.Name, id = TodoModule.IdGenerator()});
		}
	}
}					