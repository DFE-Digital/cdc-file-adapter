namespace Dfe.CdcFileAdapter.Domain.Models
{
    using System;

    /// <summary>
    /// Represents a file meta data, detailing its <see cref="Location" />.
    /// </summary>
    public class FileMetaData : ModelsBase
    {
        /// <summary>
        /// Gets or sets the location of the <see cref="File" />, in terms of
        /// storage.
        /// </summary>
        public Uri Location
        {
            get;
            set;
        }
    }
}