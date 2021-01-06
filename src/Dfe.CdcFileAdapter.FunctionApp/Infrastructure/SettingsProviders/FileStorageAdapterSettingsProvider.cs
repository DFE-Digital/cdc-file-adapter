namespace Dfe.CdcFileAdapter.FunctionApp.Infrastructure.SettingsProviders
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="IFileStorageAdapterSettingsProvider" />.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class FileStorageAdapterSettingsProvider
        : IFileStorageAdapterSettingsProvider
    {
        /// <inheritdoc />
        public string FileStorageConnectionString
        {
            get
            {
                string toReturn = Environment.GetEnvironmentVariable(
                    "FileStorageConnectionString");

                return toReturn;
            }
        }

        /// <inheritdoc />
        public string FileStorageContainerName
        {
            get
            {
                string toReturn = Environment.GetEnvironmentVariable(
                    "FileStorageContainerName");

                return toReturn;
            }
        }
    }
}