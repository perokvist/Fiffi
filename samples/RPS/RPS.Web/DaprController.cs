using Dapr;
using Fiffi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fiffi.Serialization;
using System.Threading;

namespace RPS.Web
{
    public class DaprController : ControllerBase
    {
        public static SemaphoreSlim @lock = new SemaphoreSlim(1);
        private readonly GameModule module;
        private readonly ILogger<DaprController> logger;
        private readonly Func<string, Type> typeResolver;

        public DaprController(
            GameModule module,
            ILogger<DaprController> logger,
            Func<string, Type> typeResolver)
        {
            this.module = module;
            this.logger = logger;
            this.typeResolver = typeResolver;
        }

        [HttpPost("/in")]
        [Topic("in")]
        public async Task<IActionResult> InboxAsync()
        {
            await @lock.WaitAsync();
            using (var r = new StreamReader(Request.Body))
            {
                var json = await r.ReadToEndAsync();
                logger.LogInformation("Inbox got event. Payload : {json}", json);

                //var e = typeResolver.Deserialize(json);
                //logger.LogInformation("Inbox got event. {eventName}", e.GetEventName());
                //await module.WhenAsync(e);
                //logger.LogInformation("Inbox dispatched event. {eventName}", e.GetEventName());
            }
            @lock.Release();
            return Ok();
        }

    }

}
