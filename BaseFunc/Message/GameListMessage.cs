using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using Engine.Serialization;

namespace SCMPG
{
    public class GameListMessage : Message
    {
        public int ServerPriority;
        public string ServerName = "";
        public Dictionary<int, Object.SWorldInfo> WorldInfo = new Dictionary<int, Object.SWorldInfo>();

        private class Serializer : ISerializer<GameListMessage>
        {
            public void Serialize(InputArchive archive, ref GameListMessage value)
            {
                archive.Serialize("ServerPriority", ref value.ServerPriority);
                archive.Serialize("ServerName", ref value.ServerName);
                archive.SerializeDictionary("WorldInfo", value.WorldInfo);
            }

            public void Serialize(OutputArchive archive, GameListMessage value)
            {
                archive.Serialize("ServerPriority", value.ServerPriority);
                archive.Serialize("ServerName", value.ServerName);
                archive.SerializeDictionary("WorldInfo", value.WorldInfo);
            }
        }
    }
}
