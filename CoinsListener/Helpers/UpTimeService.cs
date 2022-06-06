using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Reflection;

namespace CoinsListener.Helpers
{
    /// <summary>
    /// UpTime service.
    /// </summary>
    public interface IUpTimeService
    {
        /// <summary>
        /// Time of start.
        /// </summary>
        /// <remarks>Возвращает время запуска.</remarks>
        DateTimeOffset StartTime { get; }

        /// <summary>
        /// UpTime.
        /// </summary>
        /// <remarks>Возращает продолжительно работы.</remarks>
        TimeSpan UpTime { get; }
    }

    /// <summary>
    /// UpTime options.
    /// </summary>
    public class UpTimeOptions
    {
        /// <summary>
        /// Time of start.
        /// </summary>
        /// <remarks>Возвращает время запуска.</remarks>
        public DateTimeOffset StartTime { get; internal set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// UpTime service.
    /// </summary>
    public class UpTimeService : IUpTimeService
    {
        private readonly ILogger logger;
        private readonly UpTimeOptions upTimeOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="UpTimeService"/>.
        /// </summary>
        public UpTimeService(
            ILogger<UpTimeService> logger,
            IOptions<UpTimeOptions> upTimeOptions,
            IHostApplicationLifetime applicationLifetime)
        {
            this.logger = logger;
            this.upTimeOptions = upTimeOptions?.Value;

            if (applicationLifetime != null)
            {
                applicationLifetime.ApplicationStarted.Register(OnStarted);
                applicationLifetime.ApplicationStopped.Register(OnStopped);
                applicationLifetime.ApplicationStopping.Register(OnStopping);
            }
        }

        private string GetVersion()
        {
            return $"{Assembly.GetEntryAssembly().GetName().Name} Version: {Assembly.GetEntryAssembly().GetName().Version}, Started: {StartTime:u}" +
                $", Replica: {Environment.GetEnvironmentVariable("KUBE_REPLICA_SET")}";
        }

        private void SendInformationToSlack(string message)
        {
            logger.LogInformation($"{message} {GetVersion()}");
        }

        private void OnStarted()
        {
            SendInformationToSlack("[Started]");//, color: "#58D68D");
        }

        private void OnStopping()
        {
            SendInformationToSlack("[Stopping]");//, color: "#F4D03F");
        }

        private void OnStopped()
        {
            SendInformationToSlack("[Stopped]");//, color: "#58D68D");
        }

        /// <summary>
        /// Time of start.
        /// </summary>
        public DateTimeOffset StartTime => upTimeOptions.StartTime;

        /// <summary>
        /// UpTime.
        /// </summary>
        public TimeSpan UpTime => DateTimeOffset.UtcNow - StartTime;
    }
}