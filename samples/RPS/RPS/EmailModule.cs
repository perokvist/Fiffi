using Fiffi;
using Fiffi.Modularization;
using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RPS
{
    public class EmailModule : Module
    {
        public EmailModule(
            Dispatcher<ICommand, Task> dispatcher,
            Func<IEvent[], Task> publish,
            QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
        : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static EmailModule Initialize(
            IAdvancedEventStore store,
            ISnapshotStore snapshotStore,
           Func<IEvent[], Task> pub)
            => new ModuleConfiguration<EmailModule>((c, p, q, s) => new EmailModule(c, p, q, s))
            .Command<RequestEmail>(cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new EmailRequested() }, pub))
            .Command<SendEmail>(cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new EmailSent() }, pub))
            .Projection<EmailRequested>(e => store.AppendToStreamAsync(Streams.MailQueue, new[] { e }))
            .Projection<EmailSent>(e => store.AppendToStreamAsync(Streams.MailQueue, e.ToArray()))
            .Policy(Policy.On<GamePlayed>(EMailPolicy.When))
            .Policy(Policy.On<TimePassed, MailQueue>(Streams.MailQueue, EMailPolicy.When))
            .Create(store);

        public static class Streams
        {
            public const string MailQueue = "mailqueue";
        }
    }



    public class MailQueue
    {
        public IEnumerable<object> Items { get; internal set; }
    }

    public static class EMailPolicy
    {
        public static IEnumerable<ICommand> When(GamePlayed @event)
        {
            yield return new RequestEmail
            {
                GameId = @event.GameId
            };

            yield return new RequestEmail
            {
                GameId = @event.GameId
            };
        }

        public static IEnumerable<ICommand> When(TimePassed @event, MailQueue q)
        {
            foreach (var item in q.Items)
            {
                yield return new SendEmail();
            }
        }
    }

    public class SendEmail : ICommand
    {
        public IAggregateId AggregateId => throw new NotImplementedException();

        public Guid CorrelationId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid CausationId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public class RequestEmail : ICommand
    {
        [Required]
        [ScaffoldColumn(false)]
        public Guid GameId { get; set; }

        [Required]
        public string PlayerId { get; set; }
        [Required]
        public string Title { get; set; }

        [Required]
        public int Rounds { get; set; }
        IAggregateId ICommand.AggregateId => new AggregateId(GameId);
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }



    public class EmailRequested : IEvent
    {
        public string SourceId => throw new NotImplementedException();

        public IDictionary<string, string> Meta { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public class EmailSent : IEvent
    {
        public string SourceId => throw new NotImplementedException();

        public IDictionary<string, string> Meta { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
