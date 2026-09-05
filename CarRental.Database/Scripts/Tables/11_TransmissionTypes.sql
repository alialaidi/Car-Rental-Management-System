-- =====================================================================
-- Table: TransmissionTypes
-- Purpose: Lookup table (Automatic, Manual).
-- =====================================================================
IF OBJECT_ID('dbo.TransmissionTypes', 'U') IS NOT NULL DROP TABLE dbo.TransmissionTypes;
GO

CREATE TABLE dbo.TransmissionTypes
(
    TransmissionTypeId INT IDENTITY(1,1)  NOT NULL,
    Name                NVARCHAR(50)       NOT NULL,

    CONSTRAINT PK_TransmissionTypes PRIMARY KEY CLUSTERED (TransmissionTypeId),
    CONSTRAINT UQ_TransmissionTypes_Name UNIQUE (Name)
);
GO
