CREATE TABLE dbo.Files (
    Id              INT           IDENTITY (1, 1) NOT NULL,
    EstablishmentId INT           NULL,
    SiteVisitDate   DATE          NULL,
    [FileName]        VARCHAR (50)  NOT NULL,
    FileURL         VARCHAR (MAX) NOT NULL,
    FileType        TINYINT       NULL,
    CONSTRAINT PK_Files PRIMARY KEY CLUSTERED (Id ASC)
);

