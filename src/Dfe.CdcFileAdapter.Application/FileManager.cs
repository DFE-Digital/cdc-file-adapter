namespace Dfe.CdcFileAdapter.Application
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
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
        private const string UrnGroupingName = "{0} files";
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
            int[] fallbackUrns,
            CancellationToken cancellationToken)
        {
            File toReturn = null;

            this.loggerProvider.Debug(
                $"Firstly, searching for files under {nameof(urn)} {urn}...");

            IDictionary<int, IEnumerable<File>> fileSearch =
                await this.GetUrnFilesAsync(urn, fileType, cancellationToken)
                .ConfigureAwait(false);

            IEnumerable<File> files = fileSearch[urn];

            if (files.Any())
            {
                if (files.Count() == 1)
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
                        $"More than one file available ({files.Count()} in " +
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

                if (fallbackUrns != null)
                {
                    this.loggerProvider.Info(
                        "Fallback URNs were provided, however. Pulling back " +
                        "files for fallback URNs...");

                    fileSearch = await this.GetUrnFilesAsync(
                        fallbackUrns,
                        fileType,
                        cancellationToken)
                        .ConfigureAwait(false);

                    // We only want to return a file (i.e. zip) if there are
                    // files in our search results to return.
                    bool fallbackFilesAvailable = fileSearch
                        .SelectMany(x => x.Value)
                        .Any();

                    if (fallbackFilesAvailable)
                    {
                        toReturn = this.ZipMultipleFiles(
                            urn,
                            fileSearch);
                    }
                    else
                    {
                        this.loggerProvider.Info(
                            $"No files available for the primary " +
                            $"{nameof(urn)} = {urn}, or for any " +
                            $"{nameof(fallbackUrns)}. Null will be returned.");
                    }
                }
            }

            return toReturn;
        }

        private async Task<IDictionary<int, IEnumerable<File>>> GetUrnFilesAsync(
            int urn,
            FileTypeOption fileType,
            CancellationToken cancellationToken)
        {
            IDictionary<int, IEnumerable<File>> toReturn = null;

            int[] urns = new int[] { urn };

            toReturn = await this.GetUrnFilesAsync(
                urns,
                fileType,
                cancellationToken)
                .ConfigureAwait(false);

            return toReturn;
        }

        private async Task<IDictionary<int, IEnumerable<File>>> GetUrnFilesAsync(
            int[] urns,
            FileTypeOption fileType,
            CancellationToken cancellationToken)
        {
            Dictionary<int, IEnumerable<File>> toReturn =
                new Dictionary<int, IEnumerable<File>>();

            foreach (int urn in urns)
            {
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

                toReturn.Add(urn, files);
            }

            return toReturn;
        }

        private File ZipMultipleFiles(
            int originalUrn,
            IDictionary<int, IEnumerable<File>> urnsAndFiles)
        {
            File toReturn = null;

            int urn;
            string urnFolderName = null;
            string fileName = null;
            ZipArchiveEntry zipArchiveEntry = null;
            byte[] bytes = null;
            IEnumerable<File> files = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // For each urn group...
                    foreach (KeyValuePair<int, IEnumerable<File>> urnAndFiles in urnsAndFiles)
                    {
                        urn = urnAndFiles.Key;
                        urnFolderName = string.Format(
                            CultureInfo.InvariantCulture,
                            UrnGroupingName,
                            urn);

                        files = urnAndFiles.Value;

                        foreach (File file in files)
                        {
                            fileName = $"{urnFolderName}\\{file.FileName}";

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
                }

                memoryStream.Position = 0;
                byte[] contentBytes = memoryStream.ToArray();

                string zipName = string.Format(
                    CultureInfo.InvariantCulture,
                    UrnGroupingName,
                    originalUrn);

                toReturn = new File()
                {
                    ContentBytes = contentBytes,
                    ContentType = ZipMimeType,
                    FileName = $"{zipName}.zip",
                };

                this.loggerProvider.Info($"Archive generated: {toReturn}.");
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