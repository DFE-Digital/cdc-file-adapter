namespace Dfe.CdcFileAdapter.FunctionApp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Dfe.CdcFileAdapter.Application;
    using Dfe.CdcFileAdapter.Application.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders;
    using Dfe.CdcFileAdapter.FunctionApp.Infrastructure;
    using Dfe.CdcFileAdapter.FunctionApp.Infrastructure.SettingsProvider;
    using Dfe.CdcFileAdapter.Infrastructure.AzureStorage;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Azure.WebJobs.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using ILoggerProvider = Dfe.CdcFileAdapter.Domain.Definitions.ILoggerProvider;

    /// <summary>
    /// Functions startup class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc />
        public override void Configure(
            IFunctionsHostBuilder functionsHostBuilder)
        {
            if (functionsHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(functionsHostBuilder));
            }

            IServiceCollection serviceCollection =
                functionsHostBuilder.Services;

            AddLogging(serviceCollection);
            AddManagers(serviceCollection);
            AddSettingsProviders(serviceCollection);
            AddStorageAdapters(serviceCollection);
        }

        private static void AddLogging(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<ILogger>(CreateILogger)
                .AddScoped<ILoggerProvider, LoggerProvider>();
        }

        private static ILogger CreateILogger(IServiceProvider serviceProvider)
        {
            ILogger toReturn = null;

            ILoggerFactory loggerFactory =
                serviceProvider.GetService<ILoggerFactory>();

            string categoryName = LogCategories.CreateFunctionUserCategory(
                nameof(Dfe.CdcFileAdapter));

            toReturn = loggerFactory.CreateLogger(categoryName);

            return toReturn;
        }

        private static void AddManagers(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<IFileManager, FileManager>();
        }

        private static void AddSettingsProviders(
            IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<IFileStorageAdapterSettingsProvider, FileStorageAdapterSettingsProvider>();
        }

        private static void AddStorageAdapters(
            IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<IFileStorageAdapter, FileStorageAdapter>();
        }
    }
}