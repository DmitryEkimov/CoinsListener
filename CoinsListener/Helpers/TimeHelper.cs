using System;
using System.Numerics;

namespace CoinsListener.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public static class TimeHelpers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeStampToDateTime(this BigInteger unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds((double)unixTimeStamp); // .ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeStampToDateTime(this double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp); // .ToLocalTime();
            return dtDateTime;
        }

    }
}
