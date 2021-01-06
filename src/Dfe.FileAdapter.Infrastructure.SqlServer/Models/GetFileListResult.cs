namespace Dfe.FileAdapter.Infrastructure.SqlServer.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Dfe.CdcFileAdapter.Domain.Models;

    /// <summary>
    /// Result class for the <c>GetFileList</c> stored procedure.
    /// </summary>
    public class GetFileListResult : ModelsBase
    {
        /// <summary>
        /// Gets or sets the <c>SiteVisitDate</c> value.
        /// </summary>
        public DateTime SiteVisitDate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <c>FileName</c> value.
        /// </summary>
        public string FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <c>FileURL</c> value.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1056",
            Justification = "Dapper needs this to be a string.")]
        public string FileURL
        {
            get;
            set;
        }
    }
}