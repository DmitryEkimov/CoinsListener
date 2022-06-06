using CoinsListener.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;



namespace CoinsListener.Services
{
    /// <summary>
    /// 
    /// </summary>
    internal class QueueService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IServiceProvider services;
        private readonly ChannelReader<EventLog<TransferEventDTO>> reader;
        private readonly string name = "Coins listener save history to db";
        private readonly SessionHolderService sessionHolderService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logger"></param>
        /// <param name="reader"></param>
        /// <param name="sessionHolderService"></param>
        public QueueService(
            IServiceProvider services,
            ILogger<QueueService> logger,
            ChannelReader<EventLog<TransferEventDTO>> reader,
            SessionHolderService sessionHolderService) => (this.logger, this.services, this.reader, this.sessionHolderService) = (logger, services, reader, sessionHolderService);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("{name} started.", name);

            while (!cancellationToken.IsCancellationRequested)
            {
                while (await reader.WaitToReadAsync(cancellationToken))
                {
                    await DoWorkAsync();
                }
            }
            logger.LogInformation("{name} stopped.", name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task DoWorkAsync()
        {
            while (reader.TryRead(out var item))
            {

                if (item is null)
                    await FlashRequestsAsync();
                else
                {
                    logger.LogInformation("-->started save to db transfer all: {item}", item);
                    await ProcessTransferEventDTO(item);
                    logger.LogDebug("--> completed save to db transfer all: {item}", item);
                }
            }
        }

        private readonly ISet<CoinTransferAll> CoinsHistoriesAll = new HashSet<CoinTransferAll>(new CoinsHistoryComparer());

        /// <summary>
        ///    Собственно чтение Log Item
        /// </summary>
        /// <param name="logItem"></param>
        /// <returns></returns>
        private async Task ProcessTransferEventDTO(EventLog<TransferEventDTO> eventLog)
        {
            if (sessionHolderService.CancellationTokenSource.Token.IsCancellationRequested)
            {
                logger.LogWarning("ProcessTransferEventDTO cancelled");
                return;
            }
            var txHash = eventLog.Log.TransactionHash;

            var logIndex = eventLog.Log.LogIndex.ToUlong();

            var (token, web3) = sessionHolderService.ListenedTokens.Values.FirstOrDefault(t => t.Item1.ContractAddress.ToUpperInvariant() == eventLog.Log.Address.ToUpperInvariant());

            if (token is null || web3 is null)
            {
                logger.LogError("ProcessTransferEventDTO ListenedTokens dictionary cannot find {address}", eventLog.Log.Address);
                return;
            }

            logger.LogInformation("ProcessTransferEventDTO txHash {txHash},logIndex {logIndex}", txHash, logIndex);

            var blockNumber = eventLog.Log.BlockNumber;
            // чтобы базу не переспрашивать
            if (sessionHolderService.TxHash != txHash)
            {
                // сбрасываем предыдущий блок
                await FlashRequestsAsync();
                sessionHolderService.TxHash = txHash;
            }
            if (sessionHolderService.StartBlockNumber < blockNumber)
            {
                sessionHolderService.StartBlockNumber = blockNumber;
            }

            var coinTransferAll = new CoinTransferAll
            {
                TxHash = txHash,
                LogIndex = logIndex,
                TokenId = token.TokenId,
                Block = blockNumber.ToUlong(),
                Ts = await sessionHolderService.CalculateCoinsHistoryTs(blockNumber, web3),
                Method = "",
                FromAddress = eventLog.Event.From,
                ToAddress = eventLog.Event.To,
                Amount = Web3.Convert.FromWei(eventLog.Event.Value),
                ContractAddress = token.ContractAddress,
                Fee = await sessionHolderService.CalculateCoinsHistoryFee(txHash, eventLog.Event.To, web3)
            };

            await SaveRequestAsync(coinTransferAll);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task FlashRequestsAsync()
        {
            if (!CoinsHistoriesAll.Any())
                return;

            logger.LogInformation("flash transactions to transfer table ");

            using var scope = services.CreateScope();

            var groupedHistories = CoinsHistoriesAll.GroupBy(c => c.TxHash).Select(r => r.ToArray()).ToArray();
            CoinsHistoriesAll.Clear();
            foreach (var coinsHistories in groupedHistories)
            {
                logger.LogInformation("-->save to db transfer: {TxHash}", coinsHistories.First().TxHash);
            }
            logger.LogInformation("-->save to db transfer completed--");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coinsHistory"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        private async Task SaveCoinsHistoryAsync(CoinTransfer coinsHistory)
        {
            try
            {
                logger.LogDebug("coinsHistory saved {coinsHistory}", coinsHistory);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while saving coinsHistory TxHash: {TxHash}", coinsHistory.TxHash);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coinsHistoryAll"></param>
        /// <returns></returns>
        private async Task SaveRequestAsync(CoinTransferAll coinsHistoryAll)
        {
            using var scope = services.CreateScope();

            try
            {
                CoinsHistoriesAll.Add(coinsHistoryAll);

                logger.LogDebug("coinsHistoryAll saved {coinsHistory}", coinsHistoryAll);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while saving coinsHistoryAll TxHash: {TxHash}", coinsHistoryAll.TxHash);
            }
        }


    }
    /// <summary>
    /// 
    /// </summary>
    internal class CoinsHistoryComparer : IEqualityComparer<CoinTransferAll>
    {
        public bool Equals(CoinTransferAll x, CoinTransferAll y) =>
            x == y ||
            (x is not null && y is not null && x.TxHash.ToLower() == y.TxHash.ToLower() && x.LogIndex == y.LogIndex && x.ContractAddress.ToLower() == y.ContractAddress.ToLower());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(CoinTransferAll obj) => HashCode.Combine(obj.TxHash, obj.LogIndex, obj.ContractAddress);
    }
}