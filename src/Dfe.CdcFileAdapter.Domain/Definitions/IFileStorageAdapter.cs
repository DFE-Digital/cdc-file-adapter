namespace Dfe.CdcFileAdapter.Domain.Definitions
{
    using System;
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
        /// Gets a file for a given <paramref name="fileMetaData" />.
        /// </summary>
        /// <param name="fileMetaData">
        /// An instance of <see cref="FileMetaData" />.
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
        Task<File> GetFileAsync(
            FileMetaData fileMetaData,
            CancellationToken cancellationToken);
    }
}