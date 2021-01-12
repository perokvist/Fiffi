using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RPS.Web
{
    public class GameController : Controller
    {
        private readonly GameModule module;
        private readonly ILogger<GameController> logger;

        public GameController(GameModule module, ILogger<GameController> logger)
        {
            this.module = module;
            this.logger = logger;
        }

        [HttpGet("/games/new", Name = "new")]
        public IActionResult Create() => View(@"game\Create.cshtml", new CreateGame { Rounds = 1 });

        [HttpPost("/games", Name = "create")]
        [Consumes("application/x-www-form-urlencoded")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> FormCreateAsync(
            [FromForm] string playerId,
            [FromForm] string title,
            [FromForm] int rounds)
        {
            var gameId = Guid.NewGuid();
            await module.DispatchAsync(new CreateGame {
                GameId = Guid.NewGuid(),
                PlayerId = playerId,
                Title = title,
                Rounds = rounds
            });

            logger.LogInformation("Game Created - {gameId}", gameId);
            return base.Redirect($"/games");
        }

        [HttpPost("/games", Name = "create")]
        [Consumes("application/json")]
        public async Task<IActionResult> JsonCreateAsync([FromBody] CreateGame command)
        {
            var gameId = Guid.NewGuid();
            command.GameId = gameId;
            await module.DispatchAsync(command);

            logger.LogInformation("Game Created - {gameId}", gameId);
            return Created($"/games/{gameId}", gameId);
        }

        [HttpGet("/games/{gameId}/join", Name = "join")]
        public IActionResult Join() => View(@"game\Join.cshtml", new JoinGame());

        [HttpPost("/games/{gameId}/join", Name = "join")]
        [Consumes("application/x-www-form-urlencoded")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> FormJoinAsync([FromRoute]Guid gameId, [FromForm]string playerId)
        {
            await module.DispatchAsync(new JoinGame { GameId = gameId, PlayerId = playerId });

            logger.LogInformation("Game Joined by - {playerId}", playerId);
            return base.Redirect($"/games/{gameId}/play?playerId={playerId}");
        }


        [HttpPost("/games/{gameId}/join", Name = "join")]
        [Consumes("application/json")]
        public async Task<IActionResult> JsonJoinAsync([FromRoute]Guid gameId, [FromBody]JoinGame command)
        {
            command.GameId = gameId;

            await module.DispatchAsync(command);

            logger.LogInformation("Game Joined by - {gameId}", command.PlayerId);
            return NoContent();
        }

        [HttpGet("/games/{gameId}/play", Name = "play")]
        public IActionResult Play([FromQuery]string playerId) => View(@"game\Play.cshtml", new PlayGame { PlayerId = playerId });

        [HttpPost("/games/{gameId}/play", Name = "play")]
        [Consumes("application/x-www-form-urlencoded")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> PlayAsync([FromRoute]Guid gameId, [FromForm]string playerId, [FromForm]Hand hand)
        {
            await module.DispatchAsync(new PlayGame { GameId = gameId, PlayerId = playerId, Hand = hand });

            logger.LogInformation("Hand Played by - {hand}", hand);
            return base.Redirect($"/games/{gameId}");
        }

        [HttpPost("/games/{gameId}/play", Name = "play")]
        [Consumes("application/json")]
        public async Task<IActionResult> PlayAsync([FromRoute]Guid gameId, [FromBody]PlayGame command)
        {
            command.GameId = gameId;

            await module.DispatchAsync(command);

            logger.LogInformation("Hand Played by {playerId} - {hand}", command.PlayerId, command.Hand);
            return StatusCode((int)HttpStatusCode.Accepted);
        }

        [HttpGet("/games")]
        public async Task<IActionResult> IndexAsync()
        {
            var r = await module.QueryAsync(new GamesQuery());

            if (AcceptsHtml(Request.Headers))
                return View(@"game\Games.cshtml", r);

            return Ok(r);
        }

        [HttpGet("/games/{gameId}", Name = "details")]
        public async Task<IActionResult> DetailsAsync([FromRoute]Guid gameId)
        {
            var r = await module.QueryAsync(new GameQuery { GameId = gameId });

            if (AcceptsHtml(Request.Headers))
                return View(@"game\Details.cshtml", r);

            return Ok(r);
        }

        [HttpGet("/games/scores", Name = "scores")]
        public async Task<IActionResult> ScoresAsync([FromRoute]Guid gameId)
        {
            var r = await module.QueryAsync(new ScoreQuery());

            if (AcceptsHtml(Request.Headers))
                return View(@"game\Score.cshtml", r);

            return Ok(r);
        }

        private static bool AcceptsHtml(IHeaderDictionary headers)
           => headers[HeaderNames.Accept].Aggregate(new List<string>(), (l, r) =>
           {
               l.AddRange(r.Split(","));
               return l;
           }).Contains("text/html");
    }
}
