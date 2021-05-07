using System;
using System.Linq;

namespace Archiver.Core.Common
{
    /// <summary>
    /// Блок файла.
    /// </summary>
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
            Data = data?.ToArray() ?? throw new ArgumentNullException(nameof(data));
            Index = index;
            Size = Data.Length;
        }

        public override string ToString() => $"{{Block | Index: {Index}; Size: {Size}}}";
    }
}