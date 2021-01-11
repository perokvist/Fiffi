using CloudNative.CloudEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fiffi;

namespace Fiffi.CloudEvents
{
    public class EventMetaDataExtension : ICloudEventExtension
    {
        Dictionary<string, string> attributes = new();
        readonly string[] names = typeof(EventMetaData).GetProperties().Select(x => x.Name.ToLower()).ToArray();

        public EventMetaData MetaData
        {
            get => attributes.GetEventMetaData();
            set => attributes.AddMetaData(value);
        }

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            if (attributes == eventAttributes)
                return;

            attributes.ForEach(kv => eventAttributes[kv.Key] = kv.Value);

            attributes = eventAttributes.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }

        public Type GetAttributeType(string name)
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
            var typeInfo = IntrospectionExtensions.GetTypeInfo(type);

            var canBeNull = !typeInfo.IsValueType || (Nullable.GetUnderlyingType(typeInfo) != null);

            if (canBeNull && value == null)
                return true;

            if ((value.GetType().Equals(type)))
                return true;

            throw new InvalidOperationException($"Ivalid type for {key}");
        }
    }
}
