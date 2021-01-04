-- =============================================
-- Author:		Matt Middleton
-- Create date: 2021-01-04
-- =============================================
CREATE PROCEDURE dbo.GetFileList 
	@EstablishmentID INT, 
	@TypeID TINYINT
AS
BEGIN
    
	SELECT
		CAST(NULL AS INT) AS CDCFeild,
		f.EstablishmentId AS EstablishmentID,
		CAST(NULL AS VARCHAR(50)) AS SupplierKeyID,
		CAST(NULL AS VARCHAR(100)) AS EstablishmentName,
		CAST(NULL AS VARCHAR(50)) AS FileTypeDescription,
		f.SiteVisitDate,
		f.FileName,
		f.FileURL
	FROM Files as f
	WHERE
		f.EstablishmentId = @EstablishmentID
			AND
		f.FileType = @TypeID;

END