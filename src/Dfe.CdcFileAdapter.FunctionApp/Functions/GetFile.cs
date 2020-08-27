namespace Dfe.CdcFileAdapter.FunctionApp.Functions
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;

    /// <summary>
    /// Entry class for the <c>GetFile</c> function.
    /// </summary>
    public class GetFile
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="GetFile" /> class.
        /// </summary>
        public GetFile()
        {
            // Nothing for now.
        }

        /// <summary>
        /// Entry method for the <c>GetFile</c> function.
        /// </summary>
        /// <param name="httpRequest">
        /// An instance of type <see cref="HttpRequest" />.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="IActionResult" />.
        /// </returns>
        [FunctionName(nameof(GetFile))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "cdc-file/{urn}")]
            HttpRequest httpRequest,
            string urn)
        {
            IActionResult toReturn = null;

            toReturn = new OkResult();

            return toReturn;
        }
    }
}