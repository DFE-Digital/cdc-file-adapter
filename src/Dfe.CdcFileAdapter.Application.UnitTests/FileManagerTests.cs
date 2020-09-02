namespace Dfe.CdcFileAdapter.Application.UnitTests
{
    using Dfe.CdcFileAdapter.Application;
    using Dfe.CdcFileAdapter.Application.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class FileManagerTests
    {
        private Mock<IFileStorageAdapter> fileStorageAdapter;

        private FileManager fileManager;

        [TestInitialize]
        public void Arrange()
        {
            this.fileStorageAdapter = new Mock<IFileStorageAdapter>();

            Mock<ILoggerProvider> mockLoggerProvider = new Mock<ILoggerProvider>();

            this.fileManager = new FileManager(
                this.fileStorageAdapter.Object,
                mockLoggerProvider.Object);
        }

        [TestMethod]
        public async Task GetFileAsync_ReturnsNoFile_ReturnsNull()
        {
            // Arrange
            string urn = "012345";
            string type = "doc";
            CancellationToken cancellationToken = CancellationToken.None;

            File file = null;

            // Act
            file = await this.fileManager.GetFileAsync(
                urn,
                type,
                cancellationToken);

            // Assert
            Assert.IsNull(file);
        }

        [TestMethod]
        public async Task GetFileAsync_ReturnsAFile_ReturnsInstance()
        {
            // Arrange
            File file = new File()
            {
                // Nothing for now.
            };

            this.fileStorageAdapter
                .Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(file));

            string urn = "012345";
            string type = "doc";
            CancellationToken cancellationToken = CancellationToken.None;

            // Act
            file = await this.fileManager.GetFileAsync(
                urn,
                type,
                cancellationToken);

            // Assert
            Assert.IsNotNull(file);
        }
    }
}