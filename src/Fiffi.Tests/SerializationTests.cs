using Fiffi.Testing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Fiffi.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void AsType()
        {
            var e = new TestEvent("test") { Message = "test", SourceId = Guid.NewGuid().ToString() }.AddTestMetaData<TestState>(new AggregateId("test"));
            var jsonNetEvent = JsonConvert.SerializeObject(e);
            var corejsonEvent = System.Text.Json.JsonSerializer.Serialize(e, e.GetType());

            Assert.Equal(jsonNetEvent, corejsonEvent);
        }

        [Fact]
        public void InterfaceAsObject()
        {
            var e = new TestEvent("test") { Message = "test", SourceId = Guid.NewGuid().ToString() }.AddTestMetaData<TestState>(new AggregateId("test")) as IEvent;
            var jsonNetEvent = JsonConvert.SerializeObject(e, typeof(IEvent), new JsonSerializerSettings());
            var corejsonEvent = System.Text.Json.JsonSerializer.Serialize<object>(e);

            Assert.Equal(jsonNetEvent, corejsonEvent);
        }

        [Fact]
        public void ExplicitInterfaceAsObject()
        {
            var e = new ExplicitTestEvent { Message = "test" }.AddTestMetaData<TestState>(new AggregateId("test")) as IEvent;
            var jsonNetEvent = JsonConvert.SerializeObject(e, typeof(IEvent), new JsonSerializerSettings());
            var corejsonEvent = System.Text.Json.JsonSerializer.Serialize<object>(e, new JsonSerializerOptions {   });

            Assert.Equal(jsonNetEvent, corejsonEvent);
        }

        public class ExplicitTestEvent : IEvent
        {
            public string Message { get; set; }

            string IEvent.SourceId => "hej";

            IDictionary<string, string> IEvent.Meta { get; set; }
        }

     }
}
