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

namespace RPS.Web
{
    public class DaprController : ControllerBase
    {
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
            using (var r = new StreamReader(Request.Body))
            {
                var json = await r.ReadToEndAsync();
                logger.LogInformation("Inbox got event. {json}", json);
                var e = typeResolver.Deserialize(json);
                logger.LogInformation("Inbox event. {eventName}", e.GetEventName());
                logger.LogInformation("Inbox event source. {sourceId}", e.SourceId);
                logger.LogInformation("Inbox event version. {eventVersion}", e.Meta.GetEventStoreMetaData().EventVersion);
                await module.WhenAsync(e);
            }
            return Ok();
        }

    }

}
