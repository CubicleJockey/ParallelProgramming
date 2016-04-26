using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace ParallelProgramming.Playground.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
        {
            var shuffledItems = items.ToArray();
            var provider = new RNGCryptoServiceProvider();

            var lhsIndex = shuffledItems.Length;
            while (lhsIndex > 1)
            {
                var box = new byte[1];
                do
                {
                    provider.GetBytes(box);
                }while (!(box[0] < lhsIndex*(byte.MaxValue/lhsIndex)));

                var rhsIndex = (box[0] % lhsIndex);
                lhsIndex--;

                var value = shuffledItems[rhsIndex];
                shuffledItems[rhsIndex] = shuffledItems[lhsIndex];
                shuffledItems[lhsIndex] = value;
            }
            return shuffledItems;
        }
    }
}
