using Microsoft.AspNetCore.Mvc;

using CoinsListener.Helpers;

using System;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoinsListener.Controllers
{
    /// <summary>
    /// Common controller.
    /// </summary>
    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class CommonController : ControllerBase
    {
        private readonly IUpTimeService upTimeService;

        /// <summary>
        /// Initializes a new instance of <see cref="CommonController"/>.
        /// </summary>
        public CommonController(
            IUpTimeService upTimeService) => this.upTimeService = upTimeService;

        /// <summary>
        /// Get versions.
        /// </summary>
        /// <remarks>Возвращает актуальные версии продуктов.</remarks>
        [HttpGet("/api/v1/versions")]
        public VersionsResponse GetVersions()
        => new VersionsResponse
        {
            UpTime = upTimeService.UpTime,
            StartTime = upTimeService.StartTime,
            VersionApi = Assembly.GetEntryAssembly().GetName().Version.ToString(),
            RemoteIpAddress = HttpContext.Connection?.RemoteIpAddress?.ToString(),
        };
    }

    /// <summary>
    /// Versions response.
    /// </summary>
    public class VersionsResponse
    {
        /// <summary>
        /// Uptime.
        /// </summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan UpTime { get; internal set; }

        /// <summary>
        /// Start time.
        /// </summary>
        public DateTimeOffset StartTime { get; internal set; }

        /// <summary>
        /// Api version.
        /// </summary>
        public string VersionApi { get; internal set; }

        /// <summary>
        /// Remote ip address.
        /// </summary>
        public string RemoteIpAddress { get; internal set; }
    }

    internal class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}