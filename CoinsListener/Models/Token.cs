using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoinsListener.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class Token
    {
        /// <summary>
        /// 
        /// </summary>
        public string TokenId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TokenType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string NetworkId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ContractAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int DecimalPoints { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DeployAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? Created { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Listened { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [NotMapped]
        public ICollection<CoinTransfer> CoinsHistory { get; set; } = new HashSet<CoinTransfer>();

        /// <summary>
        /// 
        /// </summary>
        [NotMapped]
        public ICollection<CoinTransferAll> CoinsHistoryAll { get; set; } = new HashSet<CoinTransferAll>();
    }
}
