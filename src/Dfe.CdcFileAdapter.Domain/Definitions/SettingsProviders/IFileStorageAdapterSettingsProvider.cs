namespace Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders
{
    /// <summary>
    /// Describes the operations of the <see cref="IFileStorageAdapter" />
    /// settings provider.
    /// </summary>
    public interface IFileStorageAdapterSettingsProvider
    {
        /// <summary>
        /// Gets the file storage connections string.
        /// </summary>
        string FileStorageConnectionString
        {
            get;
        }

        /// <summary>
        /// Gets the file storage container name.
        /// </summary>
        string FileStorageContainerName
        {
            get;
        }
    }
}