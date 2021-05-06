using System;

namespace Archiver.Core.Common
{
    public class FileBlock
    {
        /// <summary>
        /// Индекс блока.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Размер блока.
        /// </summary>
        public readonly int Size;

        /// <summary>
        /// Данные.
        /// </summary>
        public readonly byte[] Data;

        public FileBlock(int index, byte[] data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Index = index;
            Size = data.Length;
        }

        public override string ToString() => $"{{Block | Index: {Index}; Size: {Size}}}";
    }
}