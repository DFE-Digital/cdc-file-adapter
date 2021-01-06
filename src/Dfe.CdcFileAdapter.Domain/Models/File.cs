namespace Dfe.CdcFileAdapter.Domain.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a file, as stored in storage.
    /// </summary>
    public class File : ModelsBase
    {
        /// <summary>
        /// Gets or sets the underlying content type.
        /// </summary>
        public string ContentType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content of the file, as an instance of type
        /// <see cref="IEnumerable{Byte}" />.
        /// </summary>
        public IEnumerable<byte> ContentBytes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the original filename.
        /// </summary>
        public string FileName
        {
            get;
            set;
        }
    }
}