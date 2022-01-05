﻿using Arcus.Security.Core.Caching.Configuration;
using Arcus.Templates.AzureFunctions.Databricks.JobMetrics;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Arcus.Templates.AzureFunctions.Databricks.JobMetrics
{
    public class Startup : FunctionsStartup
    {
        // This method gets called by the runtime. Use this method to configure the app configuration.
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddEnvironmentVariables();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IConfiguration config = builder.GetContext().Configuration;

            builder.ConfigureSecretStore(stores =>
            {
                //[#if DEBUG]
                stores.AddConfiguration(config);
                //[#endif]

                stores.AddEnvironmentVariables();

                //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
            });

            builder.Services.AddLogging(loggingBuilder => ConfigureLogging(loggingBuilder, config));
        }

        private static void ConfigureLogging(ILoggingBuilder builder, IConfiguration config)
        {
            var instrumentationKey = config.GetValue<string>("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY");
            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithComponentName("Azure Databricks Metrics Scraper")
                .Enrich.WithVersion()
                .WriteTo.Console()
                .WriteTo.AzureApplicationInsights(instrumentationKey);

            builder.ClearProvidersExceptFunctionProviders()
                .AddSerilog(logConfiguration.CreateLogger(), dispose: true);
        }
    }
}
