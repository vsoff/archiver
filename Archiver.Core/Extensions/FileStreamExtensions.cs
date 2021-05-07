using System;
using System.IO;

namespace Archiver.Core.Extensions
{
    public static class FileStreamExtensions
    {
        /// <summary>
        /// Записывает длину байт массива и его значения.
        /// </summary>
        public static void WriteArray(this FileStream stream, byte[] array)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (array == null) throw new ArgumentNullException(nameof(array));
            var arrayLength = BitConverter.GetBytes(array.Length);
            stream.Write(arrayLength);
            stream.Write(array, 0, array.Length);
        }

        /// <summary>
        /// Вычитывает длину байт массива и его значения.
        /// </summary>
        public static byte[] ReadArray(this FileStream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            byte[] arrayLengthBytes = new byte[4];
            stream.Read(arrayLengthBytes, 0, arrayLengthBytes.Length);
            var arrayLength = BitConverter.ToInt32(arrayLengthBytes);
            var array = new byte[arrayLength];
            stream.Read(array, 0, array.Length);
            return array;
        }
    }
}