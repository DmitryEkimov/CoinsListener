using CoinsListener.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Nethereum.Contracts;
using Nethereum.Web3;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CoinsListener.Services
{
    /// <summary>
    /// 
    /// </summary>
    public partial class BackgroundCoinsHistoryService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IServiceProvider services;
        private readonly ChannelWriter<EventLog<TransferEventDTO>> writer;
        private readonly string name = "Coins listener etherscan";
        private readonly SessionHolderService sessionHolderService;
        private readonly TokenSubscriptionServiceFactory subscriptionServiceFactory;

        public BackgroundCoinsHistoryService
        (ILogger<BackgroundCoinsHistoryService> logger, IServiceProvider services,
            ChannelWriter<EventLog<TransferEventDTO>> writer, SessionHolderService sessionHolderService, TokenSubscriptionServiceFactory subscriptionServiceFactory) =>
            (this.logger, this.services, this.writer, this.sessionHolderService, this.subscriptionServiceFactory) =
            (logger, services, writer, sessionHolderService, subscriptionServiceFactory);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="сancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken сancellationToken)
        {
            logger.LogInformation("===== START SERVICE {name} =====", name);

            //   С какими токенами будем работать
            var listenedTokens = await sessionHolderService.GetListenedTokens(сancellationToken);

            if (!listenedTokens.Any())
            {
                logger.LogWarning("{name} not avalable tokens for listen. CHECK DATABASE!!!", name);
                // нет токенов для прослушивания,
                return;
                //await Task.Delay(TimeSpan.FromHours(1), сancellationToken).ConfigureAwait(false);
                //continue;
            }

            ImmutableArray<TokenSubscriptionService> tokenSubscriptionServices = Array.Empty<TokenSubscriptionService>().ToImmutableArray();

            while (!сancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("{name} external cicle started.", name);

                var linkedCancellationToken = CreatingManualCancellationToken(сancellationToken);

                await DisposeTokenSubscriptionServices(tokenSubscriptionServices);

                await InitialLogRead(linkedCancellationToken);

                tokenSubscriptionServices = await CreateSubscriptions(listenedTokens, linkedCancellationToken);

                logger.LogInformation("{name} external cicle processed. Wait cancellation", name);
                await Run(linkedCancellationToken);
            }
            logger.LogInformation("{name} stopped.", name);
        }


        internal CancellationToken CreatingManualCancellationToken(CancellationToken сancellationToken)
        {
            logger.LogInformation("creating manual cancellation token");
            var cancellationTokenSource = new CancellationTokenSource();
            sessionHolderService.CancellationTokenSource = cancellationTokenSource;

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(сancellationToken, cancellationTokenSource.Token);

            return linkedTokenSource.Token;
        }

        internal async Task InitialLogRead(CancellationToken сancellationToken)
        {
            try
            {
                logger.LogInformation("{name} internal cicle started.", name);
                //  с какого блока будем читать 
                if (sessionHolderService.StartBlockNumber == 0L)
                {
                    sessionHolderService.StartBlockNumber = await GetStartBlockNumberInNetwork(sessionHolderService.ListenedTokens.First().Key, сancellationToken);
                }
                //   какой блок сейчас последний 
                sessionHolderService.LatestBlockNumber = await GetLatestBlockNumberInNetwork(sessionHolderService.ListenedTokens.First().Key, сancellationToken);

                // проверяем, работает ли сервис Infora - если нет, то спим 1 час
                if (sessionHolderService.LatestBlockNumber == 0L)
                {
                    logger.LogWarning("{name} problem connecting to Infura. Waiting 1 hour", name);
                    await Task.Delay(TimeSpan.FromHours(1), сancellationToken).ConfigureAwait(false);
                    return;
                }
                else if (sessionHolderService.LatestBlockNumber > sessionHolderService.StartBlockNumber)
                {
                    logger.LogInformation("-------- starting step  block numbers {startBlockNumber}-{latestBlockNumber}",
                    sessionHolderService.StartBlockNumber, sessionHolderService.LatestBlockNumber);
                    // цикл по токенам
                    foreach (var key in sessionHolderService.ListenedTokens.Keys)
                    {
                        await GetLogChangesInRangeAsync(sessionHolderService.StartBlockNumber, sessionHolderService.LatestBlockNumber, key, сancellationToken);
                    }
                    // flash database 
                    await writer.WriteAsync(null, сancellationToken);
                    logger.LogInformation("-------- step processed block numbers {startBlockNumber}-{latestBlockNumber}",
                    sessionHolderService.StartBlockNumber, sessionHolderService.LatestBlockNumber);
                }
                else // нет новых блоков
                {
                    logger.LogInformation("{name} ExecuteAsync. no any new blocks");
                }
                logger.LogInformation("{name} internal cicle processed.", name);
            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException)
            {
                logger.LogError("{name}  internal ПРЕВЫШЕН ЛИМИТ ЗАПРОСОВ к INFURA!!!");
                await Task.Delay(TimeSpan.FromHours(1), сancellationToken).ConfigureAwait(false);
                return;
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("{name}  internal cicle cancelled.", name);
                return;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "{name} internal cicle ERROR! {ex.Message}", name, ex.Message);
            }
        }

        private const int MinimumBlockConfirmations = 3; //12;

        /// <summary>
        ///    Цикл чтения Log Items в диапазоне от fromBlockNumber до toBlockNumber
        /// </summary>
        /// <param name="fromBlockNumber"></param>
        /// <param name="toBlockNumber"></param>
        /// <param name="contractAddress"></param>
        /// <param name="сancellationToken"></param>
        /// <returns></returns>
        internal async ValueTask GetLogChangesInRangeAsync(BigInteger fromBlockNumber, BigInteger toBlockNumber, string contractAddress, CancellationToken сancellationToken)
        {
            try
            {
                var (token, web3) = sessionHolderService.ListenedTokens[contractAddress];
                //if we need to stop the processor mid execution - call cancel on the token
                //crawl the required block range
                var processor = web3.Processing.Logs.CreateProcessorForContract<TransferEventDTO>(contractAddress, ProcessTransferEventDTO,
                        minimumBlockConfirmations: MinimumBlockConfirmations);
                await processor.ExecuteAsync(
                    toBlockNumber: toBlockNumber,
                    cancellationToken: сancellationToken,
                    startAtBlockNumberIfNotProcessed: fromBlockNumber).WaitAsync(сancellationToken);

            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException)
            {
                logger.LogError("GetLogChangesInRangeAsync ПРЕВЫШЕН ЛИМИТ ЗАПРОСОВ к INFURA!!!");
                throw;
            }
            catch (Nethereum.JsonRpc.Client.RpcClientTimeoutException)
            {
                logger.LogError("GetLogChangesInRangeAsync RpcClientTimeoutException.");
            }
            catch (Nethereum.JsonRpc.Client.RpcResponseException rpcex)
            {
                var errorMessage = rpcex.Message;
                logger.LogError("{GetLogChangesInRangeAsync errorMessage}", errorMessage);
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("GetLogChangesInRangeAsync task cancelled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "GetLogChangesInRangeAsync {message}", ex.Message);
            }
        }



        internal async Task<ImmutableArray<TokenSubscriptionService>> CreateSubscriptions(IDictionary<string, (Token, Web3)> listenedTokens, CancellationToken linkedCancellationToken)
        {
            logger.LogInformation("{name} create subscriptions started.", name);

            var subscriptions = new List<TokenSubscriptionService>();
            foreach (var token in listenedTokens.Values)
            {
                var server = await subscriptionServiceFactory.CreateServer(token.Item1, linkedCancellationToken);
                if (server is null)
                {
                    logger.LogError("Cannot start subscription for token {token}", token.Item1.TokenId);
                }
                else
                {
                    subscriptions.Add(server);
                }
            }

            logger.LogInformation("{name} create subscriptions finished.", name);

            return subscriptions.ToImmutableArray();
        }

        internal async ValueTask DisposeTokenSubscriptionServices(ImmutableArray<TokenSubscriptionService> tokenSubscriptionServices)
        {
            foreach (var service in tokenSubscriptionServices)
                await ((IAsyncDisposable)service).DisposeAsync();

        }

        /// <summary>
        ///   
        /// </summary>
        /// <param name="сancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken сancellationToken)
        {
            await writer.WriteAsync(null, сancellationToken);

            logger.LogInformation("{name} stop.", name);
        }

        internal async Task Run(CancellationToken cancellationToken)
        {
            // Simplification for the sake of example
            var cts = new CancellationTokenSource();

            var waitForStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            // IHostApplicationLifetime event can be used instead of `cts.Token`
            CancellationTokenRegistration registration = cts.Token.Register(() => waitForStop.SetResult());
            await using var _ = registration.ConfigureAwait(false);

            try
            {
                await waitForStop.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {

            }
        }
    }
}