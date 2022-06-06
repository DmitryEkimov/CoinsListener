using Nethereum.Signer;

using System;

namespace CoinsListener.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public static class NetworkHelpers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Chain ToChain(this string networkId) =>
            networkId.ToLowerInvariant() switch
            {
                "kovan" => Chain.Kovan,
                "ropsten" => Chain.Ropsten,
                "mainnet" => Chain.MainNet,
                "classicmainnet" => Chain.ClassicMainNet,
                "classictestnet" => Chain.ClassicTestNet,
                "morden" => Chain.Morden,
                "private" => Chain.Private,
                "rinkeby" => Chain.Rinkeby,
                "rootstockmainnet" => Chain.RootstockMainNet,
                "rootstocktestnet" => Chain.RootstockTestNet,
                _ => throw new ArgumentException("unknown networkId", nameof(networkId))
            };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns></returns>
        public static string GetRpcUrl(this (Chain networkId, string projectId) tuple) => $"https://{tuple.networkId.ToString().ToLowerInvariant()}.infura.io/v3/{tuple.projectId}";
    }
}