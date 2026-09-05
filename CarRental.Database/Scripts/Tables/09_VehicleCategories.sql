-- =====================================================================
-- Table: VehicleCategories
-- Purpose: Lookup table for vehicle categories (Economy, SUV, Luxury...).
-- =====================================================================
IF OBJECT_ID('dbo.VehicleCategories', 'U') IS NOT NULL DROP TABLE dbo.VehicleCategories;
GO

CREATE TABLE dbo.VehicleCategories
(
    CategoryId      INT IDENTITY(1,1)  NOT NULL,
    Name            NVARCHAR(100)      NOT NULL,
    Description     NVARCHAR(500)      NULL,

    CONSTRAINT PK_VehicleCategories PRIMARY KEY CLUSTERED (CategoryId),
    CONSTRAINT UQ_VehicleCategories_Name UNIQUE (Name)
);
GO
