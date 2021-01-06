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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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
        public async Task GetFileAsync_NoMetaDataFoundForUrn_ReturnsNull()
        {
            // Arrange
            int urn = 1234;
            FileTypeOption fileType = FileTypeOption.Report;
            CancellationToken cancellationToken = CancellationToken.None;

            File file = null;

            // Act
            file = await this.fileManager.GetFileAsync(
                urn,
                fileType,
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
                cancellationToken);

            // Assert
            Assert.IsNull(file);
        }

        [TestMethod]
        public async Task GetFileAsync_OneMetaDataRecordFoundForUrnFileExists_ReturnsOriginalFile()
        {
            // Arrange
            int urn = 1234;
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
                cancellationToken);

            // Assert
            Assert.AreEqual(actualFile, expectedFile);
        }

        [TestMethod]
        public async Task GetFileAsync_MultipleMetaDataRecordsFoundForUrnAllFilesExist_ReturnsZip()
        {
            // Arrange
            int urn = 1234;
            FileTypeOption fileType = FileTypeOption.Report;
            CancellationToken cancellationToken = CancellationToken.None;

            string[] exampleDocs = new string[]
            {
                "doc1.pdf",
                "doc2.pdf",
                "doc3.pdf"
            };

            Uri doc1Location = new Uri(
                "https://some-storage-container.azure.example/doc1.pdf",
                UriKind.Absolute);

            Uri doc2Location = new Uri(
                "https://some-storage-container.azure.example/doc2.pdf",
                UriKind.Absolute);

            Uri doc3Location = new Uri(
                "https://some-storage-container.azure.example/doc3.pdf",
                UriKind.Absolute);

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

            // Act
            actualFile = await this.fileManager.GetFileAsync(
                urn,
                fileType,
                cancellationToken);

            // Assert
            actualContentType = actualFile.ContentType;
            Assert.AreEqual(expectedContentType, actualContentType);

            actualFilename = actualFile.FileName;
            Assert.AreEqual(expectedFilename, actualFilename);
        }
    }
}