using System.IO;
using System.Runtime.Serialization;
using Archiver.Core.Common;

namespace Archiver.Core.Serializers
{
    public static class ArchiveHeaderSerializer
    {
        /// <summary>
        /// Проверочный заголовок формата архива.
        /// </summary>
        private static readonly byte[] FileCheckHeader = {0, 255, 234, 123, 32, 1, 1, 1, 3, 1, 55};

        private static void WriteCheckHeader(BinaryWriter writer)
        {
            foreach (var b in FileCheckHeader)
                writer.Write(b);
        }

        private static void ReadAndValidateFileCheckHeader(BinaryReader reader)
        {
            foreach (var expectedByte in FileCheckHeader)
            {
                var realByte = reader.ReadByte();
                if (realByte != expectedByte)
                    throw new SerializationException("Заголовок архива не соответствует ожидаемому");
            }
        }

        public static ArchiveHeader Deserialize(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            ReadAndValidateFileCheckHeader(reader);
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

            WriteCheckHeader(writer);
            writer.Write(block.BlocksCount);
            foreach (var offset in block.BlockOffsets)
                writer.Write(offset);

            return stream.ToArray();
        }
    }
}