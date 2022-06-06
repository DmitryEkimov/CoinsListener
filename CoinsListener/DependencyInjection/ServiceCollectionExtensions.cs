using Bastion.Coins.Api.TokenPublisher.Config;

using CoinsListener.Models;
using CoinsListener.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Nethereum.Contracts;

using System;
using System.Threading.Channels;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBackgroundService(
            this IServiceCollection services, IConfiguration configuration, Action<DbContextOptionsBuilder> optionsAction)
        {
            services.Configure<InfuraOptions>(configuration.GetSection("Infura"));
            services.Configure<NetworkOptions>(configuration.GetSection("Network"));
            services.AddHostedService<BackgroundCoinsHistoryService>();
            services.AddSingleton<TokenSubscriptionServiceFactory>();
            services.AddSingleton(Channel.CreateUnbounded<EventLog<TransferEventDTO>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }));
            services.AddSingleton<SessionHolderService>();
            services.AddSingleton(svc => svc.GetRequiredService<Channel<EventLog<TransferEventDTO>>>().Reader);
            services.AddSingleton(svc => svc.GetRequiredService<Channel<EventLog<TransferEventDTO>>>().Writer);
            services.AddHostedService<QueueService>();
            return services;
        }
    }
}