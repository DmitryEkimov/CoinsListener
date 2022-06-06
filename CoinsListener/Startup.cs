using HealthChecks.UI.Client;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Filters;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using Bastion.Coins.Api.TokenPublisher.Config;

using CoinsListener.Helpers;

using Microsoft.Extensions.Options;

namespace CoinsListener
{
    /// <summary>
    /// Startup.
    /// </summary>
    public class Startup
    {
        private static readonly Version versionAssembly = typeof(Startup).Assembly.GetName().Version;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configure services.
        /// </summary>
        /// <param name="services">Collection of service.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            string dbConnection = Configuration.GetConnectionString("DbConnection");

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddNpgSql(dbConnection, tags: new[] { "services", "db" })
                .AddCheck("services", () => HealthCheckResult.Healthy(), tags: new[] { "services" });

            services.AddControllers(options =>
            {
            }).ConfigureApiBehaviorOptions(ConfigureApiBehavior);

            services.AddMemoryCache();

            // ===== Add up time service ========
            var startedTime = DateTime.UtcNow;
            services.Configure<UpTimeOptions>(configure => { configure.StartTime = startedTime; });
            services.AddSingleton<IUpTimeService, UpTimeService>();

            services.AddBackgroundService(Configuration, options => options.UseNpgsql(dbConnection));

            // ===== Add Swagger ========
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Bastion Coins listener Service",
                    Version = versionAssembly.ToString(),
                    Description = $"Started: <b>{startedTime:u}</b>",
                });
                options.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Format = "$date-span", Example = new OpenApiString(TimeSpan.Zero.ToString()) });

                Directory.GetFiles(AppContext.BaseDirectory, "*.xml").ToList().ForEach(xmlFilePath => options.IncludeXmlComments(xmlFilePath));

                options.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
            });

            // ===== Add Cors ========
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyMethod().AllowAnyHeader().AllowCredentials();

                        string[] allowOrigins = Configuration.GetSection("AllowOrigins").Get<string[]>();
                        if (allowOrigins?.Length > 0)
                        {
                            builder.WithOrigins(allowOrigins);
                        }
                        else
                        {
                            builder.AllowAnyOrigin();
                        }
                    });
            });

            // ===== Add Forwarded headers ========
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownNetworks.Clear();
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.ForwardedForHeaderName = "X-Original-Forwarded-For";
                options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:10.0.0.0"), 104));
            });
        }

        /// <summary>
        /// Configure.
        /// </summary>
        /// <param name="app">Application builder.</param>
        public void Configure(IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();

            var infura = app.ApplicationServices.GetRequiredService<IOptions<InfuraOptions>>();

            logger.LogInformation("Start Coins listener, Infura ProjectId:{ProjectId}", infura.Value.ProjectId);
            app.ApplicationServices.GetRequiredService<IUpTimeService>();

            app.UseForwardedHeaders();

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/health/readiness", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("services"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapControllers();
            });

            app.UseSwagger().UseSwaggerUI(options =>
            {
                options.RoutePrefix = string.Empty;
                options.SwaggerEndpoint("/swagger/v1/swagger.json", $"Bastion Coins listener Service {versionAssembly}");
            });
        }

        private void ConfigureApiBehavior(ApiBehaviorOptions options)
        {
            options.InvalidModelStateResponseFactory = actionContext => BastionInvalidModelStateResponse(actionContext);
        }

        private static IActionResult BastionInvalidModelStateResponse(ActionContext actionContext)
        {
            var errorMessage = string.Join(string.Empty, actionContext.ModelState.Values.SelectMany(item => item.Errors)
                .Select(err => err.ErrorMessage + " " + err.Exception));

            var errors = actionContext.ModelState.ToDictionary(error => error.Key, error => error.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                .Where(x => x.Value.Any());

            var result = new BadRequestObjectResult(new
            {
                errors,
                Code = 107,
                Message = "Input validation error.",
                Description = $"Sorry, it didn't work this time. {errorMessage} Please correct the error(s) and try again.",
            });
            result.ContentTypes.Add(MediaTypeNames.Application.Json);
            return result;
        }
    }
}