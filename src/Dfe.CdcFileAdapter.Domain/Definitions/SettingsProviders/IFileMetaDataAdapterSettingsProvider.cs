namespace Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders
{
    /// <summary>
    /// Describes the operations of the <see cref="IFileMetaDataAdapter" />
    /// settings provider.
    /// </summary>
    public interface IFileMetaDataAdapterSettingsProvider
    {
        /// <summary>
        /// Gets the file meta data SQL server connection string.
        /// </summary>
        string FileMetaDataConnectionString
        {
            get;
        }
    }
}