namespace Dfe.CdcFileAdapter.Infrastructure.AzureStorage
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders;
    using Dfe.CdcFileAdapter.Domain.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.File;

    /// <summary>
    /// Implements <see cref="IFileStorageAdapter" />.
    /// </summary>
    public class FileStorageAdapter : IFileStorageAdapter
    {
        private const string DefaultContentType = "application/octet-stream";

        private readonly ILoggerProvider loggerProvider;

        private readonly StorageCredentials storageCredentials;
        private readonly FileRequestOptions fileRequestOptions;
        private readonly OperationContext operationContext;

        /// <summary>
        /// Initialises a new instance of the <see cref="FileStorageAdapter" />
        /// class.
        /// </summary>
        /// <param name="fileStorageAdapterSettingsProvider">
        /// An instance of type
        /// <see cref="IFileStorageAdapterSettingsProvider" />.
        /// </param>
        /// <param name="loggerProvider">
        /// An instance of type <see cref="ILoggerProvider" />.
        /// </param>
        public FileStorageAdapter(
            IFileStorageAdapterSettingsProvider fileStorageAdapterSettingsProvider,
            ILoggerProvider loggerProvider)
        {
            if (fileStorageAdapterSettingsProvider == null)
            {
                throw new ArgumentNullException(
                    nameof(fileStorageAdapterSettingsProvider));
            }

            string fileStorageConnectionString =
                fileStorageAdapterSettingsProvider.FileStorageConnectionString;

            CloudStorageAccount cloudStorageAccount =
                CloudStorageAccount.Parse(fileStorageConnectionString);

            this.storageCredentials = cloudStorageAccount.Credentials;

            this.loggerProvider = loggerProvider;

            this.fileRequestOptions = new FileRequestOptions()
            {
                // Just default, for now.
            };

            this.operationContext = new OperationContext()
            {
                // Just default, for now.
            };
        }

        /// <inheritdoc />
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1054",
            Justification = "'URN', in this instance, does not refer to a URI.")]
        public async Task<File> GetFileAsync(
            FileMetaData fileMetaData,
            CancellationToken cancellationToken)
        {
            File toReturn = null;

            if (fileMetaData == null)
            {
                throw new ArgumentNullException(nameof(fileMetaData));
            }

            Uri location = fileMetaData.Location;

            this.loggerProvider.Info($"{nameof(location)} = {location}");

            CloudFile cloudFile = new CloudFile(
                location,
                this.storageCredentials);

            this.loggerProvider.Debug(
                $"Checking if {nameof(CloudFile)} at {location} exists...");

            bool exists = await cloudFile.ExistsAsync().ConfigureAwait(false);

            this.loggerProvider.Info($"{nameof(exists)} = {exists}");

            if (exists)
            {
                FileProperties fileProperties = cloudFile.Properties;

                string contentType = fileProperties.ContentType;

                // It appears that, the content type, whilst *available* on a
                // FileShare file, doesn't tend to be updated by file upload 
                // clients as it should.
                // This is where we pull the content type from usually.
                // Therefore, if this is required, we may need to run a script
                // over existing files, or remember to supply the content type
                // (perhaps with azcopy) on initialisation of the storage.
                if (string.IsNullOrEmpty(contentType))
                {
                    this.loggerProvider.Warning(
                        $"Default content type (\"{DefaultContentType}\") " +
                        $"being used, as no content type available for this " +
                        $"particular file.");

                    contentType = DefaultContentType;
                }
                else
                {
                    this.loggerProvider.Info(
                        $"{nameof(contentType)} = \"{contentType}\"");
                }

                long length = fileProperties.Length;

                this.loggerProvider.Info(
                    $"File at \"{location}\" exists " +
                    $"({nameof(contentType)} = \"{contentType}\", " +
                    $"{nameof(length)} = {length}). Downloading content...");

                byte[] contentBytes = new byte[length];

                await cloudFile.DownloadToByteArrayAsync(
                    contentBytes,
                    0,
                    AccessCondition.GenerateEmptyCondition(),
                    this.fileRequestOptions,
                    this.operationContext,
                    cancellationToken)
                .ConfigureAwait(false);

                this.loggerProvider.Info(
                    $"{length} byte(s) downloaded. Stuffing results into a " +
                    $"{nameof(File)} instance, and returning.");

                string fileName = location.Segments.Last();

                toReturn = new File()
                {
                    ContentType = contentType,
                    ContentBytes = contentBytes,
                    FileName = fileName,
                };
            }
            else
            {
                this.loggerProvider.Warning(
                    $"File at \"{location}\" does not exist. Returning null.");
            }

            return toReturn;
        }
    }
}