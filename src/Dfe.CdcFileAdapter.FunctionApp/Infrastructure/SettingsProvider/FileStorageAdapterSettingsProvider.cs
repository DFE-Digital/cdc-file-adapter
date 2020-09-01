namespace Dfe.CdcFileAdapter.FunctionApp.Infrastructure.SettingsProvider
{
    using System;
    using Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="IFileStorageAdapterSettingsProvider" />.
    /// </summary>
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