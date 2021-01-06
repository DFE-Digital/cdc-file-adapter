namespace Dfe.CdcFileAdapter.Application
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileAdapter.Application.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Models;
    using File = Dfe.CdcFileAdapter.Domain.Models.File;

    /// <summary>
    /// Implements <see cref="IFileManager" />.
    /// </summary>
    public class FileManager : IFileManager
    {
        private const string ZipMimeType = "application/zip";

        private readonly IFileMetaDataAdapter fileMetaDataAdapter;
        private readonly IFileStorageAdapter fileStorageAdapter;
        private readonly ILoggerProvider loggerProvider;

        /// <summary>
        /// Initialises a new instance of the <see cref="FileManager" />
        /// class.
        /// </summary>
        /// <param name="fileMetaDataAdapter">
        /// An instance of type <see cref="IFileMetaDataAdapter" />.
        /// </param>
        /// <param name="fileStorageAdapter">
        /// An instance of type <see cref="IFileStorageAdapter" />.
        /// </param>
        /// <param name="loggerProvider">
        /// An instance of type <see cref="ILoggerProvider" />.
        /// </param>
        public FileManager(
            IFileMetaDataAdapter fileMetaDataAdapter,
            IFileStorageAdapter fileStorageAdapter,
            ILoggerProvider loggerProvider)
        {
            this.fileMetaDataAdapter = fileMetaDataAdapter;
            this.fileStorageAdapter = fileStorageAdapter;
            this.loggerProvider = loggerProvider;
        }

        /// <inheritdoc />
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1054",
            Justification = "'URN', in this instance, does not refer to a URI.")]
        public async Task<File> GetFileAsync(
            int urn,
            FileTypeOption fileType,
            CancellationToken cancellationToken)
        {
            File toReturn = null;

            // First, get the meta-data (i.e. location to files).
            this.loggerProvider.Debug(
                $"Pulling back file list for {nameof(urn)} = {urn} and " +
                $"{nameof(fileType)} = {fileType}...");

            IEnumerable<FileMetaData> fileMetaDatas =
                await this.fileMetaDataAdapter.GetFileMetaDatasAsync(
                    urn,
                    fileType,
                    cancellationToken)
                .ConfigureAwait(false);

            this.loggerProvider.Info(
                $"Returned {fileMetaDatas.Count()} result(s).");

            // Then, actually pull the files.
            List<File> files = new List<File>();

            File file = null;
            foreach (FileMetaData fileMetaData in fileMetaDatas)
            {
                this.loggerProvider.Debug($"Pulling {fileMetaData}...");

                file = await this.fileStorageAdapter.GetFileAsync(
                    fileMetaData,
                    cancellationToken)
                    .ConfigureAwait(false);

                if (file != null)
                {
                    this.loggerProvider.Info($"Pulled {file}.");

                    files.Add(file);
                }
                else
                {
                    this.loggerProvider.Warning(
                        $"No file could be found for {fileMetaData}!");
                }
            }

            if (files.Count > 0)
            {
                if (files.Count == 1)
                {
                    // Then just return this one file.
                    toReturn = files.Single();

                    this.loggerProvider.Info(
                        $"Only one {nameof(File)} available ({toReturn}). " +
                        $"Returning this {nameof(File)}, as-is.");
                }
                else
                {
                    this.loggerProvider.Debug(
                        $"More than one file available ({files.Count} in " +
                        $"total). Zipping up...");

                    // Else, there's more than one, and we'll need to zip them
                    // up, then return them.
                    toReturn = this.ZipMultipleFiles(urn, files);

                    this.loggerProvider.Info(
                        $"Returning zip {nameof(File)}: {toReturn}.");
                }
            }
            else
            {
                this.loggerProvider.Warning(
                    $"Could not find any files for {nameof(urn)} = {urn} " +
                    $"and {nameof(fileType)} = {fileType}.");
            }

            return toReturn;
        }

        private File ZipMultipleFiles(
            int urn,
            IEnumerable<File> files)
        {
            File toReturn = null;

            string fileName = null;
            ZipArchiveEntry zipArchiveEntry = null;
            byte[] bytes = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // For each file...
                    foreach (File file in files)
                    {
                        fileName = file.FileName;

                        // Create an entry...
                        zipArchiveEntry = zipArchive.CreateEntry(fileName);

                        using (Stream stream = zipArchiveEntry.Open())
                        {
                            bytes = file.ContentBytes.ToArray();

                            // Write to each entry with the bytes in the
                            // File...
                            stream.Write(bytes, 0, bytes.Length);
                        }

                        this.loggerProvider.Info(
                            $"Added {fileName} to the archive.");
                    }
                }

                memoryStream.Position = 0;
                byte[] contentBytes = memoryStream.ToArray();

                toReturn = new File()
                {
                    ContentBytes = contentBytes,
                    ContentType = ZipMimeType,
                    FileName = $"{urn} files.zip",
                };

                this.loggerProvider.Info($"Archive generated: {toReturn}.");
            }

            return toReturn;
        }
    }
}