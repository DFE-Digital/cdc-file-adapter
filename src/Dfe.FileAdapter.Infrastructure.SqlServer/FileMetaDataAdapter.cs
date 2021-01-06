namespace Dfe.FileAdapter.Infrastructure.SqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using Dfe.CdcFileAdapter.Domain.Definitions;
    using Dfe.CdcFileAdapter.Domain.Definitions.SettingsProviders;
    using Dfe.CdcFileAdapter.Domain.Models;
    using Dfe.FileAdapter.Infrastructure.SqlServer.Models;

    /// <summary>
    /// Implements <see cref="IFileMetaDataAdapter" />.
    /// </summary>
    public class FileMetaDataAdapter : IFileMetaDataAdapter
    {
        private const string SprocNameGetFileList = "GetFileList";

        private readonly IFileMetaDataAdapterSettingsProvider fileMetaDataAdapterSettingsProvider;
        private readonly ILoggerProvider loggerProvider;

        /// <summary>
        /// Initialises a new instance of the
        /// <see cref="FileMetaDataAdapter" /> class.
        /// </summary>
        /// <param name="fileMetaDataAdapterSettingsProvider">
        /// An instance of type
        /// <see cref="IFileMetaDataAdapterSettingsProvider" />.
        /// </param>
        /// <param name="loggerProvider">
        /// An instance of type <see cref="ILoggerProvider" />.
        /// </param>
        public FileMetaDataAdapter(
            IFileMetaDataAdapterSettingsProvider fileMetaDataAdapterSettingsProvider,
            ILoggerProvider loggerProvider)
        {
            this.fileMetaDataAdapterSettingsProvider = fileMetaDataAdapterSettingsProvider;
            this.loggerProvider = loggerProvider;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<FileMetaData>> GetFileMetaDatasAsync(
            int establishmentId,
            FileTypeOption fileType,
            CancellationToken cancellationToken)
        {
            IEnumerable<FileMetaData> toReturn = null;

            byte typeId = (byte)fileType;

            var parameters = new
            {
                EstablishmentID = establishmentId,
                TypeID = typeId,
            };

            IEnumerable<GetFileListResult> getFileListResults = null;
            using (SqlConnection sqlConnection = await this.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                this.loggerProvider.Debug(
                    $"Executing \"{SprocNameGetFileList}\" with " +
                    $"{nameof(establishmentId)} = {establishmentId}, " +
                    $"{nameof(typeId)} = {typeId}...");

                getFileListResults =
                    await sqlConnection.QueryAsync<GetFileListResult>(
                        "GetFileList",
                        parameters,
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);

                this.loggerProvider.Info(
                    $"{getFileListResults.Count()} result(s) returned.");
            }

            if (getFileListResults.Any())
            {
                // Quiz the SiteVisitDate column. Get the most recent date.
                DateTime mostRecentDate = getFileListResults
                    .Select(x => x.SiteVisitDate)
                    .Max();

                this.loggerProvider.Debug(
                    $"Mapping all results with " +
                    $"{nameof(GetFileListResult.SiteVisitDate)} = " +
                    $"\"{mostRecentDate}\"...");

                toReturn = getFileListResults
                    .Where(x => x.SiteVisitDate == mostRecentDate)
                    .Select(x =>
                    {
                        FileMetaData fileMetaData = null;

                        string fileUrl = x.FileURL;
                        string fileName = x.FileName;

                        Uri baseUri = new Uri(fileUrl, UriKind.Absolute);
                        Uri relativeUri = new Uri(fileName, UriKind.Relative);

                        Uri location = new Uri(baseUri, relativeUri);

                        fileMetaData = new FileMetaData()
                        {
                            Location = location,
                        };

                        return fileMetaData;
                    });
            }
            else
            {
                toReturn = Array.Empty<FileMetaData>();

                this.loggerProvider.Info(
                    $"No records found for {nameof(establishmentId)} = " +
                    $"{establishmentId} and {nameof(fileType)} = {fileType}.");
            }

            return toReturn;
        }

        private async Task<SqlConnection> GetOpenConnectionAsync(
            CancellationToken cancellationToken)
        {
            SqlConnection toReturn = null;

            string fileMetaDataConnectionString =
                this.fileMetaDataAdapterSettingsProvider.FileMetaDataConnectionString;

            toReturn = new SqlConnection(fileMetaDataConnectionString);

            this.loggerProvider.Debug(
                $"Opening new {nameof(SqlConnection)} using " +
                $"{nameof(fileMetaDataConnectionString)}...");

            await toReturn.OpenAsync(cancellationToken).ConfigureAwait(false);

            return toReturn;
        }
    }
}