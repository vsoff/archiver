using System.IO;
using Archiver.Core.Common;

namespace Archiver.Core.Serializers
{
    public static class ArchiveHeaderSerializer
    {
        public static ArchiveHeader Deserialize(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            var blocksCount = reader.ReadInt32();
            var blockOffsets = new long[blocksCount];
            for (int i = 0; i < blocksCount; i++)
                blockOffsets[i] = reader.ReadInt64();

            return new ArchiveHeader(blockOffsets);
        }

        public static byte[] Serialize(ArchiveHeader block)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(block.BlocksCount);
            foreach (var offset in block.BlockOffsets)
                writer.Write(offset);

            return stream.ToArray();
        }
    }
}