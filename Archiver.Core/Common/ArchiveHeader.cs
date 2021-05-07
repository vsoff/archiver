using System;
using System.Collections.Generic;
using System.Linq;

namespace Archiver.Core.Common
{
    /// <summary>
    /// Заголовок архива.
    /// </summary>
    public class ArchiveHeader
    {
        /// <summary>
        /// Кол-во блоков в файле.
        /// </summary>
        public readonly int BlocksCount;

        /// <summary>
        /// Смещение каждого блока в байтах, относительно начала архива.
        /// </summary>
        public readonly IReadOnlyList<long> BlockOffsets;

        public ArchiveHeader(long[] blockOffsets)
        {
            BlockOffsets = blockOffsets?.ToArray() ?? throw new ArgumentNullException(nameof(blockOffsets));
            BlocksCount = blockOffsets.Length;
        }

        public static ArchiveHeader CreateEmpty(int blocksCount) => new ArchiveHeader(new long[blocksCount]);
    }
}