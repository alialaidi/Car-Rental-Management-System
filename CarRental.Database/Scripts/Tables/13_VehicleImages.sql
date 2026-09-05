-- =====================================================================
-- Table: VehicleImages
-- Purpose: Gallery images per vehicle.
-- =====================================================================
IF OBJECT_ID('dbo.VehicleImages', 'U') IS NOT NULL DROP TABLE dbo.VehicleImages;
GO

CREATE TABLE dbo.VehicleImages
(
    ImageId     INT IDENTITY(1,1)  NOT NULL,
    VehicleId   INT                NOT NULL,
    ImageUrl    NVARCHAR(500)      NOT NULL,
    IsPrimary   BIT                NOT NULL CONSTRAINT DF_VehicleImages_IsPrimary DEFAULT (0),

    CONSTRAINT PK_VehicleImages PRIMARY KEY CLUSTERED (ImageId),
    CONSTRAINT FK_VehicleImages_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles (VehicleId) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_VehicleImages_VehicleId ON dbo.VehicleImages (VehicleId);
GO
