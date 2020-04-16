using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RPS.Web
{
    public class GameController : ControllerBase
    {
        private readonly GameModule module;
        private readonly ILogger<GameController> logger;

        public GameController(GameModule module, ILogger<GameController> logger)
        {
            this.module = module;
            this.logger = logger;
        }

        [HttpPost("/games")]
        public async Task<IActionResult> FooAsync()
        {
            var gameId = Guid.NewGuid();
            await module.DispatchAsync(
                new CreateGame { GameId = gameId, PlayerId = "tester", Rounds = 1, Title = "test game" });

            logger.LogInformation("Game Created - {gameId}", gameId);
            return Created($"/games/{gameId}", gameId);
        }

        [HttpGet("/games")]
        public async Task<IActionResult> IndexAsync()
            => Ok(await module.QueryAsync(new GamesQuery()));

        [HttpGet("/games/{gameId}", Name  = "details")]
        public async Task<IActionResult> DetailsAsync([FromRoute]Guid gameId)
           => Ok(await module.QueryAsync(new GamesQuery()));





    }
}
