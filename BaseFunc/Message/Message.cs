using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Comms;

namespace SCMPG
{
	public abstract class Message
	{
		private static Dictionary<int, Type> MessageTypesByMessageId;

		private static Dictionary<Type, int> MessageIdsByMessageTypes;

		static Message()
		{
			MessageTypesByMessageId = new Dictionary<int, Type>();
			MessageIdsByMessageTypes = new Dictionary<Type, int>();
			TypeInfo[] array = (from t in typeof(Message).GetTypeInfo().Assembly.DefinedTypes
								where typeof(Message).GetTypeInfo().IsAssignableFrom(t)
								orderby t.Name
								select t).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				MessageTypesByMessageId[i] = array[i].AsType();
				MessageIdsByMessageTypes[array[i].AsType()] = i;
			}
		}

		public static Message Read(byte[] bytes)
		{
			BinaryReader binaryReader = new BinaryReader(new MemoryStream(bytes));
			byte key = binaryReader.ReadByte();
			if (MessageTypesByMessageId.TryGetValue(key, out var value))
			{
				Message obj = (Message)Activator.CreateInstance(value);
				obj.Read(binaryReader);
				return obj;
			}
			return null;
		}

		public static byte[] Write(Message message)
		{
			BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
			binaryWriter.Write((byte)MessageIdsByMessageTypes[message.GetType()]);
			message.Write(binaryWriter);
			return ((MemoryStream)binaryWriter.BaseStream).ToArray();
		}

		protected abstract void Read(BinaryReader reader);

		protected abstract void Write(BinaryWriter writer);

		/*protected static void WriteAddress(BinaryWriter writer, IPEndPoint address)
		{
			SocketAddress socketAddress = address.Serialize();
			byte[] array = new byte[socketAddress.Size];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = socketAddress[i];
			}
			writer.Write((byte)socketAddress.Family);
			writer.Write((byte)array.Length);
			writer.Write(array);
		}*/
	}
}