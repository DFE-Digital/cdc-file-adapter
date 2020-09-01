namespace Dfe.CdcFileAdapter.Application
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileAdapter.Application.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Models;

    /// <summary>
    /// Implements <see cref="IFileManager" />.
    /// </summary>
    public class FileManager : IFileManager
    {
        private readonly IFileStorageAdapter fileStorageAdapter;
        private readonly ILoggerProvider loggerProvider;

        /// <summary>
        /// Initialises a new instance of the <see cref="FileManager" />
        /// class.
        /// </summary>
        /// <param name="fileStorageAdapter">
        /// An instance of type <see cref="IFileStorageAdapter" />.
        /// </param>
        /// <param name="loggerProvider">
        /// An instance of type <see cref="ILoggerProvider" />.
        /// </param>
        public FileManager(
            IFileStorageAdapter fileStorageAdapter,
            ILoggerProvider loggerProvider)
        {
            this.fileStorageAdapter = fileStorageAdapter;
            this.loggerProvider = loggerProvider;
        }

        /// <inheritdoc />
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1054",
            Justification = "'URN', in this instance, does not refer to a URI.")]
        public async Task<File> GetFileAsync(
            string urn,
            string type,
            CancellationToken cancellationToken)
        {
            File toReturn = null;

            this.loggerProvider.Debug(
                $"Pulling {nameof(File)} with {nameof(urn)} = \"{urn}\" and " +
                $"{nameof(type)} = \"{type}\" from the underlying storage...");

            toReturn =
                await this.fileStorageAdapter.GetFileAsync(
                    urn,
                    type,
                    cancellationToken)
                .ConfigureAwait(false);

            if (toReturn != null)
            {
                this.loggerProvider.Info(
                    $"{nameof(File)} pulled from storage: {toReturn}.");
            }
            else
            {
                this.loggerProvider.Warning(
                    $"Could not find {nameof(File)} with {nameof(urn)} = " +
                    $"\"{urn}\" and {nameof(type)} = \"{type}\" in the " +
                    $"underlying storage.");
            }

            return toReturn;
        }
    }
}