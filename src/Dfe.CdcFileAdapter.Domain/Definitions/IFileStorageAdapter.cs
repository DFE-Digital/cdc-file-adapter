namespace Dfe.CdcFileAdapter.Domain.Definitions
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileAdapter.Domain.Models;

    /// <summary>
    /// Describes the operations of the file storage adapter.
    /// </summary>
    public interface IFileStorageAdapter
    {
        /// <summary>
        /// Gets a file for a given <paramref name="urn" /> and
        /// <paramref name="type" />.
        /// </summary>
        /// <param name="urn">
        /// The URN of the file to return.
        /// </param>
        /// <param name="type">
        /// The type of file to return (e.g. "report", or "site-plan").
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An instance of <see cref="File" />.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1054",
            Justification = "'URN', in this instance, does not refer to a URI.")]
        Task<File> GetFile(
            string urn,
            string type,
            CancellationToken cancellationToken);
    }
}