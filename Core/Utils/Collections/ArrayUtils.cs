using System;
using System.Linq;

namespace Yaw.Core.Utils.Collections
{
    /// <summary>
    /// Содержит вспомогательные методы для работы с массивами
    /// </summary>
    public static class ArrayUtils
    {
        /// <summary>
        /// Конкатенирует несколько массивов байт в один общий
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] CombineByteArrays(params byte[][] arrays)
        {
            var rv = new byte[arrays.Sum(a => a.Length)];
            var offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}
