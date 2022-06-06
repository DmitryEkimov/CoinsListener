using System.Collections.Generic;

namespace CoinsListener.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public static class EnumerableLong
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static IEnumerable<ulong> RangeTo(this ulong from, ulong to)
        {
            for (ulong i = from; i <= to; i++)
            {
                yield return i;
            }
        }
    }
}
