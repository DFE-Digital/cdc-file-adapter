﻿namespace Dfe.CdcFileAdapter.Application.Definitions
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Models;

    /// <summary>
    /// Describes the operations of the file manager.
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// Gets a file from the underlying storage.
        /// Note: will return null if the file could not be found.
        /// </summary>
        /// <param name="urn">
        /// The URN of the file to return.
        /// </param>
        /// <param name="fileType">
        /// A <see cref="FileTypeOption" /> value.
        /// </param>
        /// <param name="fallbackUrns">
        /// An array of <see cref="int" /> values to fetch if no files exist
        /// for <paramref name="urn" />.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An instance of <see cref="File" />.
        /// Note: will return null if the file could not be found.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Usage",
            "CA1054",
            Justification = "'URN', in this instance, does not refer to a URI.")]
        Task<File> GetFileAsync(
            int urn,
            FileTypeOption fileType,
            int[] fallbackUrns,
            CancellationToken cancellationToken);
    }
}