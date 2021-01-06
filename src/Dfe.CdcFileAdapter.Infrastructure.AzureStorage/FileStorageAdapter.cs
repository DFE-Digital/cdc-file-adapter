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
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Implements <see cref="IFileStorageAdapter" />.
    /// </summary>
    public class FileStorageAdapter : IFileStorageAdapter
    {
        private readonly ILoggerProvider loggerProvider;

        private readonly BlobRequestOptions blobRequestOptions;
        private readonly OperationContext operationContext;
        private readonly CloudBlobClient cloudBlobClient;
        private readonly string fileStorageContainerName;

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

            this.cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            this.fileStorageContainerName =
                fileStorageAdapterSettingsProvider.FileStorageContainerName;

            this.loggerProvider = loggerProvider;

            this.blobRequestOptions = new BlobRequestOptions()
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

            // 1) Get the container, then...
            this.loggerProvider.Debug(
                $"Getting {nameof(CloudBlobContainer)}...");

            CloudBlobContainer cloudBlobContainer =
                await this.GetContainerAsync()
                .ConfigureAwait(false);

            this.loggerProvider.Info(
                $"{nameof(CloudBlobContainer)} obtained: " +
                $"\"{cloudBlobContainer.Uri}\".");

            // 2) Use the path to get the file.
            Uri location = fileMetaData.Location;

            bool exists = true;

            ICloudBlob cloudBlob = null;

            this.loggerProvider.Debug(
                $"Getting {nameof(ICloudBlob)} reference/checking if file " +
                $"exists at {location}...");
            try
            {
                cloudBlob =
                    await this.cloudBlobClient.GetBlobReferenceFromServerAsync(
                        location,
                        AccessCondition.GenerateEmptyCondition(),
                        this.blobRequestOptions,
                        this.operationContext)
                    .ConfigureAwait(false);

                this.loggerProvider.Info($"File exists at: {location}.");
            }
            catch (StorageException storageException)
            {
                this.loggerProvider.Warning(
                    $"It's highly likely that this file ({location}) does " +
                    $"not exist. Cannot download.",
                    storageException);

                exists = false;
            }

            CloudBlockBlob cloudBlockBlob = (CloudBlockBlob)cloudBlob;

            if (exists)
            {
                BlobProperties blobProperties = cloudBlockBlob.Properties;

                string contentType = blobProperties.ContentType;
                long length = blobProperties.Length;

                this.loggerProvider.Info(
                    $"File at \"{location}\" exists " +
                    $"({nameof(contentType)} = \"{contentType}\", " +
                    $"{nameof(length)} = {length}). Downloading content...");

                byte[] contentBytes = new byte[length];

                await cloudBlockBlob.DownloadToByteArrayAsync(
                    contentBytes,
                    0,
                    AccessCondition.GenerateEmptyCondition(),
                    this.blobRequestOptions,
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

        private async Task<CloudBlobContainer> GetContainerAsync()
        {
            CloudBlobContainer toReturn = null;

            string container = this.fileStorageContainerName;

            this.loggerProvider.Debug(
                $"Getting container reference for \"{container}\"...");

            toReturn = this.cloudBlobClient.GetContainerReference(container);

            await toReturn.CreateIfNotExistsAsync().ConfigureAwait(false);

            this.loggerProvider.Info(
                $"Container reference for \"{container}\" obtained.");

            return toReturn;
        }
    }
}