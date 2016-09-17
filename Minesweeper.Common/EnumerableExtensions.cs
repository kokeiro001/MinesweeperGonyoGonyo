using System;
using System.Collections.Generic;
using System.Linq;

namespace Minesweeper.Common
{
    public static class EnumerableExtensions
    {
        // HACK: シャッフルする拡張を持つNuGetパッケージ探す。あるじゃろ。

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list, int seed)
        {
            Random rnd = new Random(seed);
            T[] result = list.ToArray();
            for(int i = result.Length - 1; i > 1; i--)
            {
                int j = rnd.Next(0, i);
                T a = result[i];
                result[i] = result[j];
                result[j] = a;
            }
            return result;
        }
    }
}
