using System;

namespace NetDiscordRpc.Core.Exceptions
{
    public class StringOutOfRangeException: Exception
    {
        public int MaximumLength { get; private set; }
        
        public int MinimumLength { get; private set; }
        
        internal StringOutOfRangeException(string message,  int min, int max): base(message)
        {
            MinimumLength = min;
            MaximumLength = max;
        }
        
        internal StringOutOfRangeException(int minumum, int max): this("Length of string is out of range. Expected a value between " + minumum + " and " + max, minumum, max) { }
        
        internal StringOutOfRangeException(int max): this("Length of string is out of range. Expected a value with a maximum length of " + max, 0, max) { }
    }
}