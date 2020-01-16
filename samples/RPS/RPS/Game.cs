using Fiffi;
using System;
using System.Collections.Generic;

namespace RPS
{
    public static class Game
    {
        public static IEnumerable<IEvent> Handle(CreateGame command, GameState state)
         => new List<IEvent>
                       {
                           new GameCreated {
                               SourceId = command.GameId.ToString(),
                               PlayerId = command.PlayerId,
                               Title = command.Title,
                               FirstTo = command.FirstTo,
                               Created = DateTime.UtcNow }
                       };
    }

    public class GameState
    {
        public Guid Id { get; set; }
    }

    public class GameCreated : IEvent
    {
        public string SourceId { get; set; }
        public IDictionary<string, string> Meta { get; set; }
        public string PlayerId { get; set; }
        public string Title { get; set; }
        public int FirstTo { get; set; }
        public DateTime Created { get; set; }
    }

    public class CreateGame : ICommand
    {
        public Guid GameId { get; set; }
        public string PlayerId { get; set; }
        public string Title { get; set; }
        public int FirstTo { get; set; }
        IAggregateId ICommand.AggregateId => new AggregateId(GameId);
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }
}
