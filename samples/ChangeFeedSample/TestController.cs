using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ChangeFeedSample
{
    public class TestController : Controller
    {
        private readonly SampleModule module;
        private readonly ILogger<TestController> logger;

        public TestController(SampleModule module, ILogger<TestController> logger)
        {
            this.module = module;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            try
            {
                await this.module.DispatchAsync(new Fiffi.Testing.TestCommand(Guid.NewGuid()));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error dispatching command");
                return StatusCode(500);
            }
            return Ok();
        }
    }
}
