using Engine.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCMPG.Object
{
    public class SWorldInfo : Message
    {
        public string DirectoryName = string.Empty;

        public long Size;
        public DateTime LastSaveTime = new DateTime();
        public string SerializationVersion = string.Empty;

        private class Serializer : ISerializer<SWorldInfo>
        {
            public void Serialize(InputArchive archive, ref SWorldInfo value)
            {
                archive.Serialize("DirectoryName", ref value.DirectoryName);
                archive.Serialize("Size", ref value.Size);
                long a = 0;
                archive.Serialize("LastSaveTime", ref a);
                value.LastSaveTime = new DateTime(a);
                archive.Serialize("SerializationVersion", ref value.SerializationVersion);
            }

            public void Serialize(OutputArchive archive, SWorldInfo value)
            {
                archive.Serialize("DirectoryName", value.DirectoryName);
                archive.Serialize("Size", value.Size);
                archive.Serialize("LastSaveTime", value.LastSaveTime.Ticks);
                archive.Serialize("SerializationVersion", value.SerializationVersion);
            }
        }
    }
}
