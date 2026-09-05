-- =====================================================================
-- Table: FuelTypes
-- Purpose: Lookup table (Petrol, Diesel, Electric, Hybrid...).
-- =====================================================================
IF OBJECT_ID('dbo.FuelTypes', 'U') IS NOT NULL DROP TABLE dbo.FuelTypes;
GO

CREATE TABLE dbo.FuelTypes
(
    FuelTypeId  INT IDENTITY(1,1)  NOT NULL,
    Name        NVARCHAR(50)       NOT NULL,

    CONSTRAINT PK_FuelTypes PRIMARY KEY CLUSTERED (FuelTypeId),
    CONSTRAINT UQ_FuelTypes_Name UNIQUE (Name)
);
GO
