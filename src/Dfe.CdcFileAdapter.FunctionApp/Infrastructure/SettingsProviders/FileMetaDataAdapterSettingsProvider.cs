namespace Dfe.CdcFileAdapter.FunctionApp.Infrastructure.SettingsProviders
{
    using System;
    using Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders;

    /// <summary>
    /// Implements <see cref="IFileMetaDataAdapterSettingsProvider" />.
    /// </summary>
    public class FileMetaDataAdapterSettingsProvider
        : IFileMetaDataAdapterSettingsProvider
    {
        /// <inheritdoc />
        public string FileMetaDataConnectionString
        {
            get
            {
                string toReturn = Environment.GetEnvironmentVariable(
                    "FileMetaDataConnectionString");

                return toReturn;
            }
        }
    }
}