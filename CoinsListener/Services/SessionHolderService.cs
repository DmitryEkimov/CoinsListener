using Bastion.Coins.Api.TokenPublisher.Config;


using CoinsListener.Helpers;
using CoinsListener.Models;


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace CoinsListener.Services
{
    public class SessionHolderService
    {
        private readonly IServiceProvider services;
        private readonly InfuraOptions infuraOptions;
        private readonly ILogger<SessionHolderService> logger;

        public SessionHolderService(IServiceProvider services, IOptions<InfuraOptions> infuraOptions, ILogger<SessionHolderService> logger)
            => (this.services, this.infuraOptions, this.logger) = (services, infuraOptions.Value, logger);


        public CancellationTokenSource CancellationTokenSource { get; set; } = null;
        public BigInteger StartBlockNumber { get; set; } = BigInteger.Zero;
        public BigInteger LatestBlockNumber { get; set; } = BigInteger.Zero;
        public string TxHash { get; set; } = string.Empty;
        /// <summary>
        ///    список токенов с networkId которые мы  слушаем
        /// </summary>
        public readonly IDictionary<string, (Token, Web3)> ListenedTokens = new ConcurrentDictionary<string, (Token, Web3)>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="сancellationToken"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, (Token, Web3)>> GetListenedTokens(CancellationToken сancellationToken)
        {
            using var scope = services.CreateScope();

            try
            {
                ListenedTokens.Clear();
                //await foreach (var token in tokens.WithCancellation(сancellationToken))
                //{
                //    logger.LogInformation("listen token:{id} network:{network} address:{address}", token.TokenId, token.NetworkId, token.ContractAddress);
                //    //string rpcUrl = (token.NetworkId.ToChain(), infuraOptions.ProjectId).GetRpcUrl();
                //    //logger.LogInformation("GetLogChangesInRangeAsync infura url {url}", rpcUrl);
                //    //create our processor to retrieve transfers
                //    //restrict the processor to Transfers for a specific contract address
                //    //ListenedTokens.Add(token.ContractAddress.ToLower(), (token, web3));
                //}
            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException)
            {
                logger.LogError("GetListenedTokens ПРЕВЫШЕН ЛИМИТ ЗАПРОСОВ к INFURA!!!");
                throw;
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("GetListenedTokens task canceled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "GetListenedTokens {message}", ex.Message);
            }
            return ListenedTokens;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockNumber"></param>
        /// <param name="web3"></param>
        /// <returns></returns>
        public async Task<DateTime?> CalculateCoinsHistoryTs(HexBigInteger blockNumber, Web3 web3)
        {
            try
            {
                var block = await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(blockNumber).WaitAsync(CancellationTokenSource.Token);
                return block.Timestamp.Value.UnixTimeStampToDateTime();
            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException)
            {
                logger.LogError("CalculateCoinsHistoryTs ПРЕВЫШЕН ЛИМИТ ЗАПРОСОВ к INFURA!!!");
                throw;
            }
            catch (Nethereum.JsonRpc.Client.RpcClientTimeoutException)
            {
                logger.LogError("CalculateCoinsHistoryTs RpcClientTimeoutException.");
                return null;
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException rpcex)
            {
                var errorMessage = rpcex.Message;
                logger.LogError("{CalculateCoinsHistoryTs errorMessage}", errorMessage);
                return null;
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("CalculateCoinsHistoryTs task canceled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "CalculateCoinsHistoryTs {message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="toAddress"></param>
        /// <param name="web3"></param>
        /// <param name="сancellationToken"></param>
        /// <returns></returns>
        public async Task<decimal?> CalculateCoinsHistoryFee(string transactionHash, string toAddress, Web3 web3)
        {
            try
            {
                if (toAddress == "0x77A7768A9e8BAB6c774a2D8238a4797feE413003" || toAddress == "0x830535e78EF6714dC53286Ac6829dF93A96F0e6b")
                    return null;

                var transactionReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).WaitAsync(CancellationTokenSource.Token);

                var gasPrice = Web3.Convert.FromWei(transactionReceipt.EffectiveGasPrice.Value);

                var gasUsed = Web3.Convert.FromWei(transactionReceipt.GasUsed.Value);

                return gasUsed * 1000000000.0m * gasPrice * 1000000000.0m;
            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException)
            {
                logger.LogError("CalculateCoinsHistoryFee ПРЕВЫШЕН ЛИМИТ ЗАПРОСОВ к INFURA!!!");
                throw;
            }
            catch (Nethereum.JsonRpc.Client.RpcClientTimeoutException)
            {
                logger.LogError("CalculateCoinsHistoryFee RpcClientTimeoutException.");
                return null;
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException rpcex)
            {
                var errorMessage = rpcex.Message;
                logger.LogError("{CalculateCoinsHistoryFee errorMessage}", errorMessage);
                return null;
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("CalculateCoinsHistoryFee task canceled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "CalculateCoinsHistoryFee {message}", ex.Message);
                return null;
            }
        }
    }
}
