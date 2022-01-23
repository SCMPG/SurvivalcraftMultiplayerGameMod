using System;
using System.IO;
using Engine;
using Engine.Serialization;

namespace SCMPG
{
    public class GameListMessage : Message
    {
        public int ServerPriority;
        public string ServerName;

        private class Serializer : ISerializer<GameListMessage>
        {
            public void Serialize(InputArchive archive, ref GameListMessage value)
            {
                archive.Serialize("ServerPriority", ref value.ServerPriority);
                archive.Serialize("ServerName", ref value.ServerName);
            }

            public void Serialize(OutputArchive archive, GameListMessage value)
            {
                archive.Serialize("ServerPriority", value.ServerPriority);
                archive.Serialize("ServerName", value.ServerName);
            }
        }
    }
}
