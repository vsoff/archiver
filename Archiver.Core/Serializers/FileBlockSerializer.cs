using System.IO;
using Archiver.Core.Common;

namespace Archiver.Core.Serializers
{
    public static class FileBlockSerializer
    {
        public static FileBlock Deserialize(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            var index = reader.ReadInt32();
            var dataLength = reader.ReadInt32();
            var dataBytes = reader.ReadBytes(dataLength);

            return new FileBlock(index, dataBytes);
        }

        public static byte[] Serialize(FileBlock block)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(block.Index);
            writer.Write(block.Size);
            writer.Write(block.Data);

            return stream.ToArray();
        }
    }
}