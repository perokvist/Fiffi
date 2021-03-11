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
        readonly string[] names = typeof(EventStoreMetaData).GetProperties().Select(x => x.Name.ToLower()).ToArray();

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
                .Where(kv => names.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString());
        }

        public Type? GetAttributeType(string name)
        {
            if (!names.Contains(name))
                return null;

            return typeof(string); 
        }

        public bool ValidateAndNormalize(string key, ref dynamic value)
        {
            if (!attributes.ContainsKey(key))
                return false;

            var type = typeof(string);
      
            if (value == null)
                return false;

            if (value.GetType().Equals(type))
                return true;

            throw new InvalidOperationException($"Ivalid type for {key}");
        }
    }
}
