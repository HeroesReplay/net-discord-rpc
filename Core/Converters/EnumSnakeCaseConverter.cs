using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace NetDiscordRpc.Core.Converters
{
    internal class EnumSnakeCaseConverter: JsonConverter
    {

        public override bool CanConvert(Type objectType) => objectType.IsEnum;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return null;

            object val;
            return TryParseEnum(objectType, (string)reader.Value, out val) ? val : existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var enumtype = value.GetType();
            var name = Enum.GetName(enumtype, value);
            
            var members = enumtype.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (var m in members)
            {
                if (!m.Name.Equals(name)) continue;
                
                var attributes = m.GetCustomAttributes(typeof(EnumValueAttribute), true);
                if (attributes.Length > 0)
                {
                    name = ((EnumValueAttribute)attributes[0]).Value;
                }
            }

            writer.WriteValue(name);
        }
        
        public static bool TryParseEnum(Type enumType, string str, out object obj)
        {
            if (str == null)
            {
                obj = null;
                return false;
            }	
            
            var type = enumType;
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments().First();
            }

            if (!type.IsEnum)
            {
                obj = null;
                return false;
            }
            
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (var m in members)
            {
                var attributes = m.GetCustomAttributes(typeof(EnumValueAttribute), true);
                if (!attributes.Cast<EnumValueAttribute>().Any(enumval => str.Equals(enumval.Value))) continue;
                
                obj = Enum.Parse(type, m.Name, true);

                return true;
            }
            
            obj = null;
            return false;
        }
    }
}