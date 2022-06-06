using CoinsListener.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Nethereum.Contracts;

using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace CoinsListener.Services
{
    /// <summary>
    /// 
    /// </summary>
    public partial class BackgroundCoinsHistoryService
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<BigInteger> GetLatestBlockNumberInNetwork(string contractAddress, CancellationToken cancellationToken)
        {
            var web3 = sessionHolderService.ListenedTokens[contractAddress].Item2;

            try
            {
                return (await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().WaitAsync(cancellationToken)).Value;
            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException)
            {
                logger.LogError("GetLatestBlockNumber ПРЕВЫШЕН ЛИМИТ ЗАПРОСОВ к INFURA!!!");
                throw;
            }
            catch (Nethereum.JsonRpc.Client.RpcClientTimeoutException)
            {
                logger.LogError("GetLatestBlockNumber RpcClientTimeoutException.");
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException rpcex)
            {
                var errorMessage = rpcex.Message;
                logger.LogError("{GetLatestBlockNumber errorMessage}", errorMessage);
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("GetLatestBlockNumber task canceled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "GetLatestBlockNumber {message}", ex.Message);
            }

            return BigInteger.Zero;
        }

        /// <summary>
        ///
        /// </summary>
        private const ulong defaultStartBlock = 12233100;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="сancellationToken"></param>
        /// <returns></returns>
        private async Task<BigInteger> GetStartBlockNumberInNetwork(string contractAddress, CancellationToken сancellationToken)
        {

            using var scope = services.CreateScope();

            try
            {
                var startBlock = 0L;
                return new BigInteger(startBlock);
            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException)
            {
                logger.LogError("GetStartBlockNumberInNetwork ПРЕВЫШЕН ЛИМИТ ЗАПРОСОВ к INFURA!!!");
                throw;
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("GetStartBlockNumberInNetwork task canceled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "GetStartBlockNumberInNetwork {message}", ex.Message);
            }

            return defaultStartBlock;
        }

        private async Task ProcessTransferEventDTO(EventLog<TransferEventDTO> logItem)
        => await writer.WriteAsync(logItem, sessionHolderService.CancellationTokenSource.Token);

    }
}