using CloudNative.CloudEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fiffi;
using System.Diagnostics.CodeAnalysis;

namespace Fiffi.CloudEvents
{
    public class EventStoreMetaDataExtension : ICloudEventExtension
    {
        Dictionary<string, string?> attributes = new();
        readonly PropertyInfo[] names = typeof(EventStoreMetaData).GetProperties().ToArray();

        public EventStoreMetaData MetaData
        {
            get => attributes.GetEventStoreMetaData();
            set => attributes.AddStoreMetaData(value);
        }

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            
            if (attributes == eventAttributes)
                return;

            attributes
                .ForEach(kv => eventAttributes[kv.Key] = kv.Value);

            attributes = eventAttributes
                //.Where(kv => names.Any(x => x.Name.ToLower() == kv.Key))
                .ToDictionary(kv => kv.Key.ToLower(), kv => kv.Value?.ToString());
        }

        public Type GetAttributeType(string name)
        {
            var r = names.SingleOrDefault(x => x.Name.ToLower() == name)?.PropertyType;
            return r == null ? throw new ArgumentNullException("boom") : r;
        }

        public bool ValidateAndNormalize(string key, ref dynamic value)
        {
            if (!names.Any(x => x.Name.ToLower() == key))
                return false;

            var types = new [] { typeof(string), typeof(int) };
      
            if (value == null)
                return false;

            var t = value.GetType();
            if (types.Any(x => t.Equals(x)))
                return true;

            throw new InvalidOperationException($"Ivalid type for {key}");
        }
    }
}
