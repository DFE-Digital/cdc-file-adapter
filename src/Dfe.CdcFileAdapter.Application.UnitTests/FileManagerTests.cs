namespace Dfe.CdcFileAdapter.Application.UnitTests
{
    using Dfe.CdcFileAdapter.Application;
    using Dfe.CdcFileAdapter.Application.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using File = Domain.Models.File;

    [TestClass]
    public class FileManagerTests
    {
        private Mock<IFileMetaDataAdapter> mockFileMetaDataAdapter;
        private Mock<IFileStorageAdapter> mockFileStorageAdapter;

        private FileManager fileManager;

        [TestInitialize]
        public void Arrange()
        {
            this.mockFileMetaDataAdapter = new Mock<IFileMetaDataAdapter>();
            this.mockFileStorageAdapter = new Mock<IFileStorageAdapter>();

            Mock<ILoggerProvider> mockLoggerProvider = new Mock<ILoggerProvider>();

            this.fileManager = new FileManager(
                this.mockFileMetaDataAdapter.Object,
                this.mockFileStorageAdapter.Object,
                mockLoggerProvider.Object);
        }

        [TestMethod]
        public async Task GetFileAsync_NoMetaDataFoundForUrnOrFallbackUrns_ReturnsNull()
        {
            // Arrange
            int urn = 1234;
            FileTypeOption fileType = FileTypeOption.Report;
            int[] fallbackUrns = new int[] { 4567, 890 };
            CancellationToken cancellationToken = CancellationToken.None;

            File file = null;

            // Act
            file = await this.fileManager.GetFileAsync(
                urn,
                fileType,
                fallbackUrns,
                cancellationToken);

            // Assert
            Assert.IsNull(file);
        }

        [TestMethod]
        public async Task GetFileAsync_OneMetaDataRecordFoundForUrnFileMissing_ReturnsNull()
        {
            // Arrange
            int urn = 1234;
            FileTypeOption fileType = FileTypeOption.Report;
            int[] fallbackUrns = null;
            CancellationToken cancellationToken = CancellationToken.None;

            Uri location = new Uri(
                "https://some-storage-container.azure.example/doesntexist.pdf",
                UriKind.Absolute);

            FileMetaData fileMetaData = new FileMetaData()
            {
                Location = location,
            };

            IEnumerable<FileMetaData> fileMetaDatas = new FileMetaData[]
            {
                fileMetaData,
            };

            this.mockFileMetaDataAdapter
                .Setup(x => x.GetFileMetaDatasAsync(It.IsAny<int>(), It.IsAny<FileTypeOption>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(fileMetaDatas));

            File file = null;

            // Act
            file = await this.fileManager.GetFileAsync(
                urn,
                fileType,
                fallbackUrns,
                cancellationToken);

            // Assert
            Assert.IsNull(file);
        }

        [TestMethod]
        public async Task GetFileAsync_OneMetaDataRecordFoundForUrnFileExists_ReturnsOriginalFile()
        {
            // Arrange
            int urn = 1234;
            int[] fallbackUrns = null;
            FileTypeOption fileType = FileTypeOption.Report;
            CancellationToken cancellationToken = CancellationToken.None;

            Uri location = new Uri(
                "https://some-storage-container.azure.example/exists.png",
                UriKind.Absolute);

            FileMetaData fileMetaData = new FileMetaData()
            {
                Location = location,
            };

            IEnumerable<FileMetaData> fileMetaDatas = new FileMetaData[]
            {
                fileMetaData,
            };

            this.mockFileMetaDataAdapter
                .Setup(x => x.GetFileMetaDatasAsync(It.IsAny<int>(), It.IsAny<FileTypeOption>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(fileMetaDatas));

            Random random = new Random();
            byte[] contentBytes = new byte[2048];

            random.NextBytes(contentBytes);

            File expectedFile = new File()
            {
                FileName = "exists.png",
                ContentType = "image/png",
                ContentBytes = contentBytes,
            };

            this.mockFileStorageAdapter
                .Setup(x => x.GetFileAsync(It.IsAny<FileMetaData>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(expectedFile));

            File actualFile = null;

            // Act
            actualFile = await this.fileManager.GetFileAsync(
                urn,
                fileType,
                fallbackUrns,
                cancellationToken);

            // Assert
            Assert.AreEqual(actualFile, expectedFile);
        }

        [TestMethod]
        public async Task GetFileAsync_MultipleMetaDataRecordsFoundForUrnAllFilesExist_ReturnsZip()
        {
            // Arrange
            int urn = 1234;
            int[] fallbackUrns = null;
            FileTypeOption fileType = FileTypeOption.Report;
            CancellationToken cancellationToken = CancellationToken.None;

            string[] exampleDocs = new string[]
            {
                "doc1.pdf",
                "doc2.pdf",
                "doc3.pdf"
            };

            IEnumerable<FileMetaData> fileMetaDatas = exampleDocs
                .Select(x => $"https://some-storage-container.azure.example/{x}")
                .Select(x => new Uri(x, UriKind.Absolute))
                .Select(x => new FileMetaData() { Location = x })
                .ToArray();

            this.mockFileMetaDataAdapter
                .Setup(x => x.GetFileMetaDatasAsync(It.IsAny<int>(), It.IsAny<FileTypeOption>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(fileMetaDatas));

            Random random = new Random();
            byte[] contentBytes = null;

            File file = null;
            Func <FileMetaData, CancellationToken, Task<File>> getFileAsyncCallback =
                (fmd, ct) =>
                {
                    contentBytes = new byte[random.Next(1024, 2048)];

                    random.NextBytes(contentBytes);

                    file = new File()
                    {
                        FileName = fmd.Location.Segments.Last(),
                        ContentType = "application/pdf",
                        ContentBytes = contentBytes,
                    };

                    return Task.FromResult(file);
                };

            this.mockFileStorageAdapter
                .Setup(x => x.GetFileAsync(It.IsAny<FileMetaData>(), It.IsAny<CancellationToken>()))
                .Returns(getFileAsyncCallback);

            File actualFile = null;

            string expectedContentType = "application/zip";
            string actualContentType = null;

            string expectedFilename = $"{urn} files.zip";
            string actualFilename = null;

            IEnumerable<ZipArchiveEntry> zipArchiveEntries = null;
            int expectedNumberOfArchiveEntries = 3;
            int actualNumberOfArchiveEntries;

            // Act
            actualFile = await this.fileManager.GetFileAsync(
                urn,
                fileType,
                fallbackUrns,
                cancellationToken);

            // Assert
            actualContentType = actualFile.ContentType;
            Assert.AreEqual(expectedContentType, actualContentType);

            actualFilename = actualFile.FileName;
            Assert.AreEqual(expectedFilename, actualFilename);

            // -> Actually open the zip to check that the bytes are all good.
            zipArchiveEntries = this.ExtractZipEntries(actualFile);
            actualNumberOfArchiveEntries = zipArchiveEntries.Count();

            Assert.AreEqual(
                expectedNumberOfArchiveEntries,
                actualNumberOfArchiveEntries);
        }

        [TestMethod]
        public async Task GetFileAsync_NoResultsForPrimaryUrnButResultsForFallbackUrns_ReturnsZip()
        {
            // Arrange
            int urn = 1234;
            int[] fallbackUrns = new int[] { 3456, 7890 };
            FileTypeOption fileType = FileTypeOption.Report;
            CancellationToken cancellationToken = CancellationToken.None;

            Func<int, FileTypeOption, CancellationToken, Task<IEnumerable<FileMetaData>>> getFileMetaDataCallback =
                (urn, fileType, cancellationToken) =>
                {
                    IEnumerable<FileMetaData> results = null;

                    // Only return results for a fallback.
                    if (fallbackUrns.Contains(urn))
                    {
                        results = new string[]
                            {
                                $"{urn} docs/report-{urn}.pdf",
                                $"{urn} docs/{Guid.NewGuid()}.docx",
                            }
                            .Select(x => $"https://some-storage-container.azure.example/{x}")
                            .Select(x => new Uri(x, UriKind.Absolute))
                            .Select(x => new FileMetaData() { Location = x })
                            .ToArray();
                    }
                    else
                    {
                        results = Array.Empty<FileMetaData>();
                    }

                    return Task.FromResult(results);
                };

            this.mockFileMetaDataAdapter
                .Setup(x => x.GetFileMetaDatasAsync(It.IsAny<int>(), It.IsAny<FileTypeOption>(), It.IsAny<CancellationToken>()))
                .Returns(getFileMetaDataCallback);

            Random random = new Random();
            byte[] contentBytes = null;

            File file = null;
            Func<FileMetaData, CancellationToken, Task<File>> getFileAsyncCallback =
                (fmd, ct) =>
                {
                    contentBytes = new byte[random.Next(1024, 2048)];

                    random.NextBytes(contentBytes);

                    file = new File()
                    {
                        FileName = fmd.Location.Segments.Last(),
                        ContentType = "application/pdf",
                        ContentBytes = contentBytes,
                    };

                    return Task.FromResult(file);
                };

            this.mockFileStorageAdapter
                .Setup(x => x.GetFileAsync(It.IsAny<FileMetaData>(), It.IsAny<CancellationToken>()))
                .Returns(getFileAsyncCallback);

            File actualFile = null;

            string expectedContentType = "application/zip";
            string actualContentType = null;

            string expectedFilename = $"{urn} files.zip";
            string actualFilename = null;

            IEnumerable<ZipArchiveEntry> zipArchiveEntries = null;

            int expectedNumberOfArchiveEntries = 4;
            int actualNumberOfArchiveEntries;

            // Act
            actualFile = await this.fileManager.GetFileAsync(
                urn,
                fileType,
                fallbackUrns,
                cancellationToken);

            // Assert
            actualContentType = actualFile.ContentType;
            Assert.AreEqual(expectedContentType, actualContentType);

            actualFilename = actualFile.FileName;
            Assert.AreEqual(expectedFilename, actualFilename);

            // -> Actually open the zip to check that the bytes are all good.
            zipArchiveEntries = this.ExtractZipEntries(actualFile);
            actualNumberOfArchiveEntries = zipArchiveEntries.Count();

            Assert.AreEqual(
                expectedNumberOfArchiveEntries,
                actualNumberOfArchiveEntries);
        }

        private IEnumerable<ZipArchiveEntry> ExtractZipEntries(File file)
        {
            IEnumerable<ZipArchiveEntry> toReturn = null;

            byte[] zipBytes = file.ContentBytes.ToArray();

            using (MemoryStream memoryStream = new MemoryStream(zipBytes))
            {
                using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read, false))
                {
                    toReturn = zipArchive.Entries.ToArray();
                }
            }

            return toReturn;
        }
    }
}