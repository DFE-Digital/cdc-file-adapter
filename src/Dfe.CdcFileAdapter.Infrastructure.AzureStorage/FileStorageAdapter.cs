namespace Dfe.CdcFileAdapter.Infrastructure.AzureStorage
{
    using System;
    using System.Diagnostics.CodeAnalysis;
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
        public async Task<File> GetFile(
            string urn,
            string type,
            CancellationToken cancellationToken)
        {
            File toReturn = null;

            // 1) Get the container, then...
            this.loggerProvider.Debug(
                $"Getting {nameof(CloudBlobContainer)}...");

            CloudBlobContainer cloudBlobContainer =
                await this.GetContainerAsync()
                .ConfigureAwait(false);

            this.loggerProvider.Info(
                $"{nameof(CloudBlobContainer)} obtained: " +
                $"\"{cloudBlobContainer.Uri}\".");

            // 2) Get the directory reference, then...
            this.loggerProvider.Debug(
                $"Getting {nameof(CloudBlobDirectory)} with {nameof(type)} " +
                $"= \"{type}\"...");

            CloudBlobDirectory cloudBlobDirectory =
                cloudBlobContainer.GetDirectoryReference(type);

            this.loggerProvider.Info(
                $"{nameof(CloudBlobDirectory)} obtained: " +
                $"\"{cloudBlobDirectory.Uri}\".");

            // 3) Try and find the file...!
            this.loggerProvider.Debug(
                $"Getting {nameof(CloudBlob)} with {nameof(urn)} = " +
                $"\"{urn}\"...");

            CloudBlob cloudBlob = cloudBlobDirectory.GetBlobReference(urn);

            Uri uri = cloudBlob.Uri;

            this.loggerProvider.Info(
                $"{nameof(CloudBlob)} obtained: \"{uri}\".");

            // Does this guy exist?
            this.loggerProvider.Debug(
                $"Checking for existance of \"{uri}\"...");

            bool exists =
                await cloudBlob.ExistsAsync(
                    this.blobRequestOptions,
                    this.operationContext,
                    cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                BlobProperties blobProperties = cloudBlob.Properties;

                string contentType = blobProperties.ContentType;
                long length = blobProperties.Length;

                this.loggerProvider.Info(
                    $"File at \"{uri}\" exists ({nameof(contentType)} = " +
                    $"\"{contentType}\", {nameof(length)} = {length}). " +
                    $"Downloading content...");

                byte[] contentBytes = new byte[length];

                await cloudBlob.DownloadToByteArrayAsync(contentBytes, 0)
                    .ConfigureAwait(false);

                this.loggerProvider.Info(
                    $"{length} byte(s) downloaded. Stuffing results into a " +
                    $"{nameof(File)} instance, and returning.");

                toReturn = new File()
                {
                    ContentType = contentType,
                    ContentBytes = contentBytes,
                };
            }
            else
            {
                this.loggerProvider.Warning(
                    $"File at \"{uri}\" does not exist. Returning null.");
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