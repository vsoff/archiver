using Archiver.Core.Common;
using Archiver.Core.Serializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Archiver.Tests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void FileBlockSerializerTest()
        {
            var oldBlock = new FileBlock(666, new byte[] {1, 2, 3, 3, 4});
            var bytes = FileBlockSerializer.Serialize(oldBlock);
            var newBlock = FileBlockSerializer.Deserialize(bytes);

            Assert.AreEqual(oldBlock.Index, newBlock.Index);
            Assert.AreEqual(oldBlock.Size, newBlock.Size);
            for (int i = 0; i < oldBlock.Data.Length; i++)
                Assert.AreEqual(oldBlock.Data[i], newBlock.Data[i]);
        }

        [TestMethod]
        public void ArchiveHeaderSerializerTest()
        {
            var oldArchiveHeader = new ArchiveHeader(new long[] {1, 2, 333, 3, 1114, 23, 2, 99});
            var bytes = ArchiveHeaderSerializer.Serialize(oldArchiveHeader);
            var newArchiveHeader = ArchiveHeaderSerializer.Deserialize(bytes);

            Assert.AreEqual(oldArchiveHeader.BlocksCount, newArchiveHeader.BlocksCount);
            for (int i = 0; i < oldArchiveHeader.BlockOffsets.Count; i++)
                Assert.AreEqual(oldArchiveHeader.BlockOffsets[i], newArchiveHeader.BlockOffsets[i]);
        }
    }
}