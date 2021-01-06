namespace Dfe.CdcFileAdapter.Domain.Definitions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Dfe.CdcFileAdapter.Domain.Models;

    /// <summary>
    /// Describes the operations of the file meta adapter.
    /// </summary>
    public interface IFileMetaDataAdapter
    {
        /// <summary>
        /// Gets file meta data for a given <paramref name="urn" />
        /// and <paramref name="fileType" />.
        /// </summary>
        /// <param name="urn">
        /// The urn, as an <see cref="int" /> value.
        /// </param>
        /// <param name="fileType">
        /// A <see cref="FileTypeOption" /> value.
        /// </param>
        /// <param name="cancellationToken">
        /// An instance of <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// An instance of type <see cref="IEnumerable{FileMetaData}" />.
        /// </returns>
        Task<IEnumerable<FileMetaData>> GetFileMetaDatasAsync(
            int urn,
            FileTypeOption fileType,
            CancellationToken cancellationToken);
    }
}