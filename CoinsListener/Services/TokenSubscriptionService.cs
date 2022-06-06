using Bastion.Coins.Api.TokenPublisher.Config;

using CoinsListener.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nethereum.Contracts;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Reactive.Eth.Subscriptions;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CoinsListener.Services
{
    public class TokenSubscriptionServiceFactory
    {
        private readonly ChannelWriter<EventLog<TransferEventDTO>> writer;
        private readonly InfuraOptions infuraOptions;
        private readonly ILogger<TokenSubscriptionService> logger;

        public TokenSubscriptionServiceFactory
        (ChannelWriter<EventLog<TransferEventDTO>> writer,
            IOptions<InfuraOptions> infuraOptions,
            ILogger<TokenSubscriptionService> logger)
            => (this.writer, this.infuraOptions, this.logger) = (writer, infuraOptions.Value, logger);

        public async Task<TokenSubscriptionService> CreateServer(Token token, CancellationToken cancellationToken)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var server = new TokenSubscriptionService(writer, infuraOptions, logger, cancellationToken);
            try
            {
                await server.Start(token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "start  subscription error {message}", ex.Message);
                await ((IAsyncDisposable)server).DisposeAsync();
                return null;
            }

            return server;
        }
    }

    public class TokenSubscriptionService : IAsyncDisposable
    {
        private StreamingWebSocketClient client = null;
        private EthLogsObservableSubscription subscription = null;
        private readonly ChannelWriter<EventLog<TransferEventDTO>> writer;
        private readonly InfuraOptions infuraOptions;
        private readonly ILogger logger;
        private CancellationToken cancellationToken;

        public TokenSubscriptionService
            (ChannelWriter<EventLog<TransferEventDTO>> writer,
            InfuraOptions infuraOptions,
            ILogger<TokenSubscriptionService> logger, CancellationToken cancellationToken)
            => (this.writer, this.infuraOptions, this.logger, this.cancellationToken) = (writer, infuraOptions, logger, cancellationToken);

        public async Task Start(Token token)
        {
            try
            {
                logger.LogInformation("start substription to token {tokenId}", token.TokenId);
                var networkId = token.NetworkId.ToString().ToLowerInvariant();
                client = new StreamingWebSocketClient($"wss://{networkId}.infura.io/ws/v3/{infuraOptions.ProjectId}");
                // create a log filter specific to Transfers
                // this filter will match any Transfer (matching the signature) regardless of address
                var filterTransfers = Event<TransferEventDTO>.GetEventABI().CreateFilterInput(token.ContractAddress);
                // create the subscription
                // it won't do anything yet
                subscription = new EthLogsObservableSubscription(client);

                // attach a handler for Transfer event logs
                subscription.GetSubscriptionDataResponsesAsObservable().Subscribe(ActionOnSubscriptionDataResponse,
                    ActionOnSubscriptionErrorResponse, cancellationToken);
                StreamingWebSocketClient.ForceCompleteReadTotalMilliseconds = int.MaxValue;

                // open the web socket connection
                await client.StartAsync();
                // begin receiving subscription data
                // data will be received on a background thread
                await subscription.SubscribeAsync(filterTransfers);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "start  subscription error {message}", ex.Message);
                throw;
            }
            logger.LogInformation("substription to token {tokenId} is succesful", token.TokenId);
        }

        internal void ActionOnSubscriptionErrorResponse(Exception ex)
            => logger.LogError(ex, " LogTokenTransferObservableSubscrtion error {message}", ex.Message);

        internal async void ActionOnSubscriptionDataResponse(FilterLog filterLog)
        {
            try
            {
                // decode the log into a typed event log
                var decoded = Event<TransferEventDTO>.DecodeEvent(filterLog);
                if (decoded is not null)
                {
                    logger.LogInformation("log transfer from:{from} to: {to} amount:{value} TxHash:{TxHash}", decoded.Event.From, decoded.Event.To, decoded.Event.Value, filterLog.TransactionHash);
                    await writer.WriteAsync(new EventLog<TransferEventDTO>(decoded.Event, filterLog), cancellationToken);
                }
                else
                {
                    // the log may be an event which does not match the event
                    // the name of the function may be the same
                    // but the indexed event parameters may differ which prevents decoding
                    logger.LogInformation("Found not standard transfer log");
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("Log Address: {Address} is not a standard transfer log: {message}", filterLog.Address, ex.Message);
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await subscription?.UnsubscribeAsync();
            subscription = null;
            client?.Dispose();
            client = null;
        }
    }
}
