using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoinsListener.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CoinTransfer
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

        /// <summary>
        /// 
        /// </summary>
        [NotMapped]
        public Token Token { get; set; }

        public static explicit operator CoinTransfer(CoinTransferAll v) =>
            new()
            {
                TxHash = v.TxHash,
                LogIndex = v.LogIndex,
                TokenId = v.TokenId,
                Method = v.Method,
                Block = v.Block,
                Ts = v.Ts,
                FromAddress = v.FromAddress,
                ToAddress = v.ToAddress,
                Amount = v.Amount,
                Fee = v.Fee,
                ContractAddress = v.ContractAddress
            };
    }
}
