namespace Dfe.CdcFileAdapter.FunctionApp.Functions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileAdapter.Application.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;

    /// <summary>
    /// Entry class for the <c>GetFile</c> function.
    /// </summary>
    public class GetFile
    {
        private readonly IFileManager fileManager;
        private readonly ILoggerProvider loggerProvider;

        /// <summary>
        /// Initialises a new instance of the <see cref="GetFile" /> class.
        /// </summary>
        /// <param name="fileManager">
        /// An instance of type <see cref="IFileManager" />.
        /// </param>
        /// <param name="loggerProvider">
        /// An instance of type <see cref="ILoggerProvider" />.
        /// </param>
        public GetFile(
            IFileManager fileManager,
            ILoggerProvider loggerProvider)
        {
            this.fileManager = fileManager;
            this.loggerProvider = loggerProvider;
        }

        /// <summary>
        /// Entry method for the <c>GetFile</c> function.
        /// </summary>
        /// <param name="httpRequest">
        /// An instance of type <see cref="HttpRequest" />.
        /// </param>
        /// <param name="urn">
        /// The URN of the file to return.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="IActionResult" />.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1054",
            Justification = "'URN', in this instance, does not refer to a URI.")]
        [FunctionName(nameof(GetFile))]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "cdc-file/{urn}")]
            HttpRequest httpRequest,
            string urn,
            CancellationToken cancellationToken)
        {
            IActionResult toReturn = null;

            this.loggerProvider.Debug(
                $"File requested. {nameof(urn)} = \"{urn}\".");

            // urn never can be null; if someone tries to call ./cdc-file or
            // ./cdc-file/, a 404 is returned by default.
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            string type = httpRequest.Query["type"];

            // We do however, need a null-check on type.
            if (!string.IsNullOrEmpty(type))
            {
                this.loggerProvider.Debug($"{nameof(type)} = \"{type}\"");

                Domain.Models.File file =
                    await this.fileManager.GetFileAsync(
                        urn,
                        type,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (file != null)
                {
                    this.loggerProvider.Info(
                        $"The method " +
                        $"{nameof(IFileManager)}.{nameof(IFileManager.GetFileAsync)} " +
                        $"method returned {file} for {nameof(urn)} = " +
                        $"\"{urn}\" and {nameof(type)} = \"{type}\" - " +
                        $"returning with a {nameof(FileContentResult)}.");

                    byte[] contentBytes = file.ContentBytes.ToArray();
                    string contentType = file.ContentType;

                    toReturn = new FileContentResult(
                        contentBytes,
                        contentType);
                }
                else
                {
                    this.loggerProvider.Warning(
                        $"The method " +
                        $"{nameof(IFileManager)}.{nameof(IFileManager.GetFileAsync)} " +
                        $"method returned null for {nameof(urn)} = " +
                        $"\"{urn}\" and {nameof(type)} = \"{type}\" - " +
                        $"returning {nameof(NotFoundResult)}.");

                    toReturn = new NotFoundResult();
                }
            }
            else
            {
                this.loggerProvider.Warning(
                    $"The {nameof(type)} was not supplied. This is " +
                    $"required. Returning {nameof(BadRequestResult)}.");

                // Return "bad request" to indicate we need it.
                toReturn = new BadRequestResult();
            }

            return toReturn;
        }
    }
}