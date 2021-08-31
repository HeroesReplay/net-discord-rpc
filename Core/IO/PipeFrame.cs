using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace NetDiscordRpc.Core.IO
{
    public struct PipeFrame
    {
	    public static readonly int MAX_SIZE = 16 * 1024;
	    public OpCode Opcode { get; set; }
	    public uint Length => (uint) Data.Length;
	    public byte[] Data { get; set; }
		
		public string Message
		{
			get => GetMessage();
			set => SetMessage(value);
		}
		
		public PipeFrame(OpCode opcode, object data)
		{
			Opcode = opcode;
			Data = null;
			
			SetObject(data);
		}
		
		public static Encoding MessageEncoding => Encoding.UTF8;

		private void SetMessage(string str) => Data = MessageEncoding.GetBytes(str);
		
		private string GetMessage() => MessageEncoding.GetString(Data);
		
		public void SetObject(object obj) => SetMessage(JsonConvert.SerializeObject(obj));

		public void SetObject(OpCode opcode, object obj)
		{
			Opcode = opcode;
			SetObject(obj);
		}
		
		public T GetObject<T>() => JsonConvert.DeserializeObject<T>(GetMessage());

		public bool ReadStream(Stream stream)
		{
			uint op;
			if (!TryReadUInt32(stream, out op)) return false;
			
			uint len;
			if (!TryReadUInt32(stream, out len)) return false;

			var readsRemaining = len;
			
			using (var mem = new MemoryStream())
			{
				var buffer = new byte[Min(2048, len)];
				int bytesRead;
				
				while ((bytesRead = stream.Read(buffer, 0, Min(buffer.Length, readsRemaining))) > 0)
				{
					readsRemaining -= len;
					mem.Write(buffer, 0, bytesRead);
				}

				var result = mem.ToArray();
				if (result.LongLength != len) return false;

				Opcode = (OpCode)op;
				Data = result;
				return true;
			}
		}
		
		private static int Min(int a, uint b)
		{
			if (b >= a) return a;
			return (int) b;
		}
        
		private static bool TryReadUInt32(Stream stream, out uint value)
		{
			var bytes = new byte[4];
			var cnt = stream.Read(bytes, 0, bytes.Length);
			
			if (cnt != 4)
			{
				value = default;
				return false;
			}

            value = BitConverter.ToUInt32(bytes, 0);
			return true;
		}   
		
		public void WriteStream(Stream stream)
		{
			var op = BitConverter.GetBytes((uint) Opcode);
			var len = BitConverter.GetBytes(Length);
			
			var buff = new byte[op.Length + len.Length + Data.Length];
			op.CopyTo(buff, 0);
			len.CopyTo(buff, op.Length);
			Data.CopyTo(buff, op.Length + len.Length);
			
			stream.Write(buff, 0, buff.Length);
		}		
    }
}