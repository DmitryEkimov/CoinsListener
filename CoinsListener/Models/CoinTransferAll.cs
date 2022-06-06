using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoinsListener.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CoinTransferAll
    {
        /// <summary>
        /// 
        /// </summary>
        public string TxHash { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong LogIndex { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TokenId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong Block { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? Ts { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FromAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ToAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal? Amount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal? Fee { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ContractAddress { get; set; }

        [NotMapped]
        public Token Token { get; set; }
    }
}
