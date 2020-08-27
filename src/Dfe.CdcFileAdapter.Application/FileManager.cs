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
        private readonly ILoggerProvider loggerProvider;

        /// <summary>
        /// Initialises a new instance of the <see cref="FileManager" />
        /// class.
        /// </summary>
        /// <param name="loggerProvider">
        /// An instance of type <see cref="ILoggerProvider" />.
        /// </param>
        public FileManager(ILoggerProvider loggerProvider)
        {
            this.loggerProvider = loggerProvider;
        }

        /// <inheritdoc />
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1054",
            Justification = "'URN', in this instance, does not refer to a URI.")]
        public Task<File> GetFile(
            string urn,
            string type,
            CancellationToken cancellationToken)
        {
            // TODO: Just stubbed out for now - actually call storage.
            File file = null;

            if (type == "report")
            {
                this.loggerProvider.Debug("Creating stub file...");

                file = new File()
                {
                    ContentType = "text/plain",
                    ContentBytes = Convert.FromBase64String("VGhpcyBpcyBzb21lIHRlc3QgZGF0YS4="),
                };

                this.loggerProvider.Info($"Returning stub {file}.");
            }
            else
            {
                this.loggerProvider.Warning(
                    $"Could not \"find\" a file of type \"{type}\". " +
                    $"Returning null.");
            }

            return Task.FromResult(file);
        }
    }
}