using System;
using System.Linq;
using System.Text;

namespace NetDiscordRpc.Core.Helpers
{
    public static class StringHelper
    {
        public static string GetNullOrString(this string str) => str.Length == 0 || string.IsNullOrEmpty(str.Trim()) ? null : str;
        
        public static bool WithinLength(this string str, int bytes) => str.WithinLength(bytes, Encoding.UTF8);
        
        public static bool WithinLength(this string str, int bytes, Encoding encoding) => encoding.GetByteCount(str) <= bytes;

        public static string ToCamelCase(this string str)
        {
            return str?.ToLower()
                .Split(new[] { "_", " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => char.ToUpper(s[0]) + s.Substring(1, s.Length - 1))
                .Aggregate(string.Empty, (s1, s2) => s1 + s2);
        }
        
        public static string ToSnakeCase(this string str)
        {
            if (str == null) return null;
            
            var concat = string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString()).ToArray());
            
            return concat.ToUpper();
        }
    }
}