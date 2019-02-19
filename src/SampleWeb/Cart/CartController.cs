using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb.Cart
{
	[Route("api/[controller]")]
	public class CartController : Controller
	{
		private readonly CartModule module;

		public CartController(CartModule module)
		{
			this.module = module;
		}

		[HttpPost]
		public async Task<IActionResult> PostAsync([FromBody] AddItemCommand command)
		{
			await module.DispatchAsync(command);
			return Ok();
		}
	}
}
